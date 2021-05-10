import socket
import sys
from tello_python import tello
import datetime
import threading
import time

now = ""
global TimeChecker

#drone = tello.Tello()
#drone.takeoff()

#A drón kontorolláló parancsok kilettek kommentelve, így drón tényleges csatlakoztatása nélkül is le lehet tesztelni a programot konzolra való kiíratás segítségével
#(Ha a drónkontroll parancsok bentmaradnak, és nincsen csatlakoztatva drón minden parancs után kb 10másodperc timeoutot kapunk)

#Drone control function
def drone_control(array):
    if(float(array[1]) > 3):
        print('Up')
#        drone.up(1)
    elif(float(array[1]) < -3):
        print('Down')
#        drone.down(1)

    if(float(array[2]) > 3):
        print('Forward')
#        drone.forward(1)
    elif(float(array[2]) < -3):
        print('Backward')
#        drone.back(1)
        
    if(float(array[0]) > 3):
        print('Left')
#        drone.left(1)
    elif(float(array[0]) < -3):
        print('Right')
#        drone.right(1)
        
    return

def check_last_message_time():
    global TimeChecker
    while (TimeChecker == True):
        if(now != ""):
            if(now + datetime.timedelta(seconds = 2)  < datetime.datetime.now()):
                print("2 secs since last control command, drone will land now!")
                #drone.land()
                TimeChecker = False
        time.sleep(2)

HOST = '192.168.44.133'
#HOST = '192.168.1.67'  
PORT = 7000

FLYING = False

s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
print('# Socket created')

# Create socket on port
try:
    s.bind((HOST, PORT))
except socket.error as msg:
    print('# Bind failed. ')
    sys.exit()

print('# Socket bind complete')

# Start listening on socket
s.listen(10)
print('# Socket now listening')

# Wait for client
conn, addr = s.accept()
print('# Connected to ' + addr[0] + ':' + str(addr[1]))


TimeChecker = True
x = threading.Thread(target=check_last_message_time)
x.start()

# Receive data from client
while True:     
    data = conn.recv(1024)
    line = data.decode('UTF-8')    # Convert to string
    line = line.replace("\n","")   # Remove newline character

    array = line.split("_")
    
    if(len(array) == 4):
        if(FLYING == False):
            print("The dorne should take off firts!")
        else:
            now = datetime.datetime.now()
            drone_control(array)

    if(array[0] == "exit"):
        print("Shutting down the server.")
        #drone.land()
        break
    elif(array[0] == "takeoff"):
        if(FLYING == True):
            print("The drone is already flying!")
        else:
            FLYING = True
            print("takeoff")
            #drone.takeoff()
    elif(array[0] == "land"):
        if(FLYING == False):
            print("The drone has landed already!")
        else:
            FLYING = False
            now = ""
            print("land")
            #drone.land()

s.close()
print("Program exited without errors")

#Waiting for the last check_last_message_time function call
now = ""
time.sleep(3)
TimeChecker = False
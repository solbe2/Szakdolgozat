package com.example.DroneControllerApplication;

import android.annotation.SuppressLint;
import android.content.Context;
import android.hardware.Sensor;
import android.hardware.SensorEvent;
import android.hardware.SensorEventListener;
import android.hardware.SensorManager;
import android.os.Bundle;
import android.util.Log;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.TextView;

import androidx.appcompat.app.AppCompatActivity;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.PrintWriter;
import java.net.Socket;
import java.net.UnknownHostException;

@SuppressLint("SetTextI18n")
public class MainActivity extends AppCompatActivity implements SensorEventListener {
    Thread Thread1 = null;
    EditText etIP, etPort;
    TextView tvMessages;
    EditText etMessage;
    Button btnSend;
    Button btnTakeoff;
    Button btnLand;
    String SERVER_IP;
    int SERVER_PORT;

    public float x;
    public float y;
    public float z;
    public boolean started = false;
    public boolean flying = false;

    private SensorManager sensorManager;
    Sensor accelerometer;

    private PrintWriter output;
    private BufferedReader input;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);
        etIP = findViewById(R.id.etIP);
        etPort = findViewById(R.id.etPort);
        tvMessages = findViewById(R.id.tvMessages);
        etMessage = findViewById(R.id.etMessage);
        btnSend = findViewById(R.id.btnSend);
        btnTakeoff = findViewById(R.id.btnTakeoff);
        btnLand = findViewById(R.id.btnLand);
        Button btnConnect = findViewById(R.id.btnConnect);

        sensorManager = (SensorManager) getSystemService(Context.SENSOR_SERVICE);
        accelerometer = sensorManager.getDefaultSensor(Sensor.TYPE_ACCELEROMETER);
        sensorManager.registerListener(MainActivity.this, accelerometer, SensorManager.SENSOR_DELAY_GAME);

        btnConnect.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                tvMessages.setText("");
                SERVER_IP = etIP.getText().toString().trim();
                SERVER_PORT = Integer.parseInt(etPort.getText().toString().trim());
                Thread1 = new Thread(new Thread1());
                Thread1.start();
            }
        });
        btnSend.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {

                Thread sendDataToServerThread = new Thread(new sendDataToServerThread());
                if(started == false) {
                    sendDataToServerThread.start();
                    started = true;
                    btnSend.setText("Stop controlling the drone");
                }
                else {
                    sendDataToServerThread.interrupt();
                    started = false;
                    btnSend.setText("Start controlling the drone");
                }
            }
        });
        btnTakeoff.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                if(started == true) {
                    output.write("takeoff_");
                }
            }
        });
        btnLand.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                if(started == true) {
                    output.write("land_");
                }
            }
        });
    }

    @Override
    public void onSensorChanged(SensorEvent event) {

        x = event.values[0];
        y = event.values[1];
        z = event.values[2];

        /*
        x = (float) (Math.atan2(event.values[1], event.values[2])/(Math.PI/180));
        y = (float) (Math.atan2(event.values[0], event.values[2])/(Math.PI/180));
        z = (float) (Math.atan2(event.values[0], event.values[1])/(Math.PI/180));

         */
    }

    @Override
    public void onAccuracyChanged(Sensor sensor, int accuracy) {
    }

    class Thread1 implements Runnable {
        public void run() {
            Socket socket;
            try {
                socket = new Socket(SERVER_IP, SERVER_PORT);
                output = new PrintWriter(socket.getOutputStream());
                input = new BufferedReader(new InputStreamReader(socket.getInputStream()));
                runOnUiThread(new Runnable() {
                    @Override
                    public void run() {
                        tvMessages.setText("Connected.\n");
                    }
                });
            }
            catch (IOException e) {
                e.printStackTrace();
            }
        }
    }

    class sendDataToServerThread implements Runnable {
        @Override
        public void run() {
            while(started == true){
                output.write(x + "_" + y + "_" + z + "_");
                output.flush();
                try {
                    Thread.sleep(50);
                } catch (InterruptedException e) {
                    e.printStackTrace();
                }
            }

            output.write("exit");
            output.flush();
        }
    }
}
# MQTT listener + stress test (Python + .NET)

Utilities for testing an MQTT broker by:
- `mqtt_stress_test.py`: publishes messages
- `mqtt_listener.py`: subscribes and prints message receive rate using python and paho-mqtt
- `mqtt_listener_mqttnet.cs`: subscribes and prints message receive rate using .NET 10 and MQTTnet
- `mqtt_listener_hivemqtt.cs`: subscribes and prints message receive rate using .NET 10 and HiveMQtt

Notes:
- Defaults are `localhost:1883`, topic `stresstest`, QoS 2.
- Publisher waits for each publish to complete; this measures end-to-end broker ACK behavior.

## Python setup

```powershell
python -m pip install -r requirements.txt
```

## Run: Python subscriber

```powershell
python .\mqtt_listener.py
```

## Run: MQTTnet subscriber

```powershell
dotnet run -c Release --file .\mqtt_listener_mqttnet.cs
```

## Run: HiveMQtt subscriber

```powershell
dotnet run -c Release --file .\mqtt_listener_hivemqtt.cs
```
## Run: Python stress test (publish)

```powershell
python .\mqtt_stress_test.py --messages 10000 --payload-bytes 200000
```
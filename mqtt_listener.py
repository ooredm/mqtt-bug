#!/usr/bin/env python3
from __future__ import annotations

import signal
import threading
import time

import paho.mqtt.client as mqtt

def main() -> int:
    host = "localhost"
    port = 1883
    topic = "stresstest"
    qos = 2

    stop = threading.Event()
    total = 0

    def on_connect(client: mqtt.Client, userdata, flags, rc, properties=None):
        if rc != 0:
            print(f"connect failed rc={rc}")
            stop.set()
            return
        client.subscribe(topic, qos=qos)
        print(f"connected {host}:{port} topic={topic}")

    def on_message(client: mqtt.Client, userdata, msg: mqtt.MQTTMessage):
        nonlocal total
        total += 1

    client = mqtt.Client(mqtt.CallbackAPIVersion.VERSION2)
    client.on_connect = on_connect
    client.on_message = on_message
    client.reconnect_delay_set(1, 30)

    signal.signal(signal.SIGINT, lambda *_: stop.set())
    client.connect(host, port, keepalive=60)
    client.loop_start()

    last = time.monotonic()
    last_n = 0
    try:
        while not stop.is_set():
            time.sleep(1)
            now = time.monotonic()
            dn = total - last_n
            dt = max(1e-9, now - last)
            print(f"total={total} +{dn} rate={dn / dt:.0f} msg/s")
            last, last_n = now, total
    finally:
        client.loop_stop()
        print(f"final total={total}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

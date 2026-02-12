#!/usr/bin/env python3
from __future__ import annotations

import argparse
import signal
import threading
import time

import paho.mqtt.client as mqtt

def main() -> int:
    host = "localhost"
    port = 1883
    topic = "stresstest"
    qos = 2
    default_messages = 8000
    concurrency = 8

    p = argparse.ArgumentParser(description="Publish a fixed-topic MQTT load stream.")
    p.add_argument("--messages", type=int, default=default_messages)
    p.add_argument("--payload-bytes", type=int, default=4096)
    args = p.parse_args()

    messages = max(0, args.messages)
    payload_bytes = max(0, args.payload_bytes)

    payload = b"x" * max(0, payload_bytes)

    stop = threading.Event()
    signal.signal(signal.SIGINT, lambda *_: stop.set())

    published = 0
    pub_lock = threading.Lock()

    def worker(worker_index: int, count: int) -> None:
        nonlocal published
        client = mqtt.Client(mqtt.CallbackAPIVersion.VERSION2)
        client.connect(host, port, keepalive=60)
        client.loop_start()
        try:
            for _ in range(count):
                if stop.is_set():
                    break
                info = client.publish(topic, payload=payload, qos=qos)
                info.wait_for_publish()
                with pub_lock:
                    published += 1
        finally:
            try:
                client.disconnect()
            except Exception:
                pass
            client.loop_stop()

    # Split work.
    per = messages // max(1, concurrency)
    rem = messages % max(1, concurrency)

    start = time.perf_counter()
    threads: list[threading.Thread] = []
    for i in range(max(1, concurrency)):
        n = per + (1 if i < rem else 0)
        t = threading.Thread(target=worker, args=(i, n), daemon=True)
        threads.append(t)
        t.start()

    for t in threads:
        t.join()

    elapsed = max(1e-9, time.perf_counter() - start)
    print(
        f"done published={published} elapsed={elapsed:.2f}s "
        f"rate={published / elapsed:.0f} msg/s topic={topic} host={host}:{port} qos={qos}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

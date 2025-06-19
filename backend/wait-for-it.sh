#!/usr/bin/env bash
# wait-for-it.sh

host="$1"
shift
port="$1"
shift

echo "⏳ Waiting for $host:$port..."

while ! nc -z $host $port; do
  sleep 1
done

echo "✅ $host:$port is up!"
exec "$@"

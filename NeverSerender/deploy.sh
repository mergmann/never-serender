#!/bin/bash

if [ -z "$2" ]; then
    exit 1
fi

echo "Parameters: $*"

SRC=$(dirname "$1")
NAME="$2"

TARGET="$(dirname $0)/../Bin64/Plugins/Local"
mkdir -p "$TARGET"

echo
echo "Deploying binary:"
echo

while true; do
    sleep 2
    echo "From \"$1\" to \"$TARGET/\""
    cp -f "$1" "$TARGET/"

    # cp -f "$SRC/System.Runtime.CompilerServices.Unsafe.dll" "$TARGET/"

    if [ $? -eq 0 ]; then
        break
    fi
done

echo "Done"
echo
exit 

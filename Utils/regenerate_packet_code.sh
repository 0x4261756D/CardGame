#!/bin/bash

set -Eeuo pipefail

if [ $# -lt 2 ]
then
	echo "Usage: $0 <path to brief> <path to Common.cs>"
	exit 1
fi

for packet in *.brief
do
	if [ $packet = 'Base.brief' ]
	then
		echo "Skipping Base.brief"
		continue
	fi
	echo "$1 ./$packet ./"
	$1 "./$packet" ./
done

cp "$2" ./

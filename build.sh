#!/bin/bash

if [ ! -f paket.lock ]; then
	dotnet paket install
fi

dotnet paket restore
dotnet fake build $@



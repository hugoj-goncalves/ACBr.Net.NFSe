#!/bin/bash

dotnet build ./src/ACBr.Net.NFSe --configuration Debug //nowarn:CS1591,CS1572
cp ./src/ACBr.Net.NFSe/obj/Debug/netstandard2.0/Eklesia.ACBr.Net.NFSe.dll ../../cortex/backend/packages/eklesia.acbr.net.nfse/1.5.1.13/lib/netstandard2.0
cp ./src/ACBr.Net.NFSe/obj/Debug/netstandard2.0/Eklesia.ACBr.Net.NFSe.pdb ../../cortex/backend/packages/eklesia.acbr.net.nfse/1.5.1.13/lib/netstandard2.0

set -e

if test "$#" -ne 1
then
	echo "No version number provided"
	exit 1
fi

name="$1_AOT"
echo "$name"
mkdir -p $name
cp -r config "$name/"
cp -r ../Core/decks "$name/"
mkdir -p "$name/artworks"
cp ../Client/artworks/default_artwork.png "$name/artworks"
mkdir -p "$name/Core"
cd ../Client/
dotnet publish -c Release -r linux-x64 --sc true /p:PublishAot=true
cp bin/Release/net8.0/linux-x64/publish/* "../Release/$name/"
cd ../Core/
dotnet publish -c Release -r linux-x64 --sc true /p:PublishAot=true
cp bin/Release/net8.0/linux-x64/publish/* "../Release/$name/Core"
cd ../Release
rm $name/Core/*.dbg
rm $name/*.dbg
tar -czf "$name.tar.gz" "$name/"

name="$1_Linux_SelfContained"
echo "$name"
mkdir $name
cp -r config "$name/"
cp -r ../Core/decks "$name/"
mkdir "$name/Core"
cd ../Client/
dotnet publish -c Release -r linux-x64 --sc true /p:PublishSingleFile=true
cp bin/Release/net8.0/linux-x64/publish/* "../Release/$name/"
rm ../Release/$name/*.dbg || true
cd ../Core/
dotnet publish -c Release -r linux-x64 --sc true /p:PublishSingleFile=true
cp bin/Release/net8.0/linux-x64/publish/* "../Release/$name/Core"
rm ../Release/$name/Core/*.dbg || true
cd ../Release
tar -czf "$name.tar.gz" "$name/"

name="$1_Linux"
echo "$name"
mkdir $name
cp -r config "$name/"
cp -r ../Core/decks "$name/"
mkdir "$name/Core"
cd ../Client/
dotnet publish -c Release -r linux-x64 --sc false /p:PublishSingleFile=true
cp bin/Release/net8.0/linux-x64/publish/* "../Release/$name/"
rm ../Release/$name/*.dbg || true
cd ../Core/
dotnet publish -c Release -r linux-x64 --sc false /p:PublishSingleFile=true
cp bin/Release/net8.0/linux-x64/publish/* "../Release/$name/Core"
rm ../Release/$name/Core/*.dbg || true
cd ../Release
tar -czf "$name.tar.gz" "$name/"

name="$1_Windows_SelfContained"
echo "$name"
mkdir $name
cp -r config "$name/"
cp -r ../Core/decks "$name/"
mkdir "$name/Core"
cd ../Client/
dotnet publish -c Release -r win-x64 --sc true /p:PublishSingleFile=true
cp bin/Release/net8.0/win-x64/publish/* "../Release/$name/"
cd ../Core/
dotnet publish -c Release -r win-x64 --sc true /p:PublishSingleFile=true
cp bin/Release/net8.0/win-x64/publish/* "../Release/$name/Core"
cd ../Release
zip -r "$name.zip" "$name/"

name="$1_Windows"
echo "$name"
mkdir $name
cp -r config "$name/"
cp -r ../Core/decks "$name/"
mkdir "$name/Core"
cd ../Client/
dotnet publish -c Release -r win-x64 --sc false /p:PublishSingleFile=true
cp bin/Release/net8.0/win-x64/publish/* "../Release/$name/"
cd ../Core/
dotnet publish -c Release -r win-x64 --sc false /p:PublishSingleFile=true
cp bin/Release/net8.0/win-x64/publish/* "../Release/$name/Core"
cd ../Release
zip -r "$name.zip" "$name/"

rm -r "$1_Linux/"
rm -r "$1_Linux_SelfContained/"
rm -r "$1_Windows/"
rm -r "$1_Windows_SelfContained/"

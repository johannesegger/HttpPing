dotnet publish -c Release -o ..\deploy .\src

$imageName = "johannesegger/http-ping"
docker build -t $imageName .
docker tag $imageName $imageName

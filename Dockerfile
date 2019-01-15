FROM microsoft/dotnet:sdk AS build-env

#PlantUml
WORKDIR /PlantUml.Net
# Copy csproj and restore as distinct layers
COPY /PlantUml.Net/*.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY /PlantUml.Net/. ./

# lib
WORKDIR /LOD-CM-LIB
# Copy csproj and restore as distinct layers
COPY /LOD-CM-LIB/*.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY /LOD-CM-LIB/. ./

# web interface
WORKDIR /web

# Copy csproj and restore as distinct layers
COPY /LOD-CM-WEB/*.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY /LOD-CM-WEB/. ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM microsoft/dotnet:aspnetcore-runtime
WORKDIR /app
COPY --from=build-env /web/out .
ENTRYPOINT ["dotnet", "LOD-CM.dll", "/data"]
# Pour construire l'image:
# docker build -t lod_cm .
# Pour lancer le container (ps: je te laisse voir pour les ports avec les ingé):
# docker run  --rm -v /data2/hamdif/doctorants/ph/xp/lod_cm:/data -d -p 84:80 --name lod_cm_web lod_cm
# docker run  --rm -v E:/download:/data -d -p 8080:80 --name lod_cm_web lod_cm
# Pour stopper et supprimer le container
# docker stop lod_cm_web
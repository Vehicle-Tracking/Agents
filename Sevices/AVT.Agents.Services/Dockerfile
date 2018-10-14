FROM microsoft/dotnet:2.1-runtime-deps-alpine3.7 AS build-env
WORKDIR /app
# Disable the invariant mode (set in base image)
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LC_ALL=en_US.UTF-8 \
    LANG=en_US.UTF-8
# Install .NET Core SDK
ENV DOTNET_SDK_VERSION 2.1.403
# Enable correct mode for dotnet watch (only mode supported in a container)
ENV DOTNET_USE_POLLING_FILE_WATCHER=true \ 
    # Skip extraction of XML docs - generally not useful within an image/container - helps perfomance
    NUGET_XMLDOC_MODE=skip
RUN apk add --no-cache --virtual .build-deps \
        openssl \
    && wget -O dotnet.tar.gz https://dotnetcli.blob.core.windows.net/dotnet/Sdk/$DOTNET_SDK_VERSION/dotnet-sdk-$DOTNET_SDK_VERSION-linux-musl-x64.tar.gz \
    && dotnet_sha512='620f091eba8d111b13d440c20926f60919e64dd421c6cbf2696b6f3f643a3d654b7dc394e6e84b1c4bef6ff872c754a7317e9b94977cbcb93b5d0fdfe08d8b55' \
    && echo "$dotnet_sha512  dotnet.tar.gz" | sha512sum -c - \
    && mkdir -p /usr/share/dotnet \
    && tar -C /usr/share/dotnet -xzf dotnet.tar.gz \
    && ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet \
    && rm dotnet.tar.gz \
    && apk del .build-deps
# Enable correct mode for dotnet watch (only mode supported in a container)
ENV DOTNET_USE_POLLING_FILE_WATCHER=true \ 
    # Skip extraction of XML docs - generally not useful within an image/container - helps performance
    NUGET_XMLDOC_MODE=skip

COPY *.csproj ./
RUN dotnet restore

WORKDIR /app
COPY . ./
RUN dotnet build -c Release .

RUN dotnet publish -c Release -o /app/out

ENTRYPOINT ["dotnet", "out/AVT.Agents.Services.dll"]

# before run please go to the app directory => cd Sevices/AVT.Services.Agents
# build the image => docker build -t agents . # '.' means the current context which is closely related to where the docker is located
# check the contents of the image, pretty cool => docker run -it --entrypoint sh  agents  #press ls to see the content or cd to go to a directory
# run simulator => docker run --entrypoint '/bin/sh' agents -c 'dotnet out/AVT.Agents.Services.dll --ServiceName simulator'
# run scheduler => docker run --entrypoint '/bin/sh' agents -c 'dotnet out/AVT.Agents.Services.dll --ServiceName scheduler'
# make sure to stop the images after testing
# just get the image id by => docker ps
# select images ids and stop them => docker stop [one or more image ids separated by comma space] # like this => docker stop 47527265c9aa, 877706df7827
# notice: you need to have already configured aws credentional in the image since i will set those configuration in cicd tools you might need to 
# do it manually once running the image locally to do that use -e option in docker run or i have made some changes in the code
# you can pass them in input argument wtih this names => aws_access_key_id, aws_secret_access_key
# please notice how we passed service name to run the container

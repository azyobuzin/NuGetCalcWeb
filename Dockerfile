FROM ubuntu:14.10
MAINTAINER azyobuzin <azyobuzin@users.sourceforge.jp>

RUN apt-key adv --keyserver keyserver.ubuntu.com --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
RUN echo "deb http://download.mono-project.com/repo/debian wheezy main" | tee /etc/apt/sources.list.d/mono-xamarin.list
RUN apt-get update
RUN apt-get install -y mono-devel ca-certificates-mono
RUN apt-get install -y nodejs npm
RUN ln -s /usr/bin/nodejs /usr/local/bin/node

COPY . /NuGetCalcWeb/

WORKDIR /NuGetCalcWeb
RUN mono .nuget/NuGet.exe restore && xbuild /p:Configuration=Release

WORKDIR /NuGetCalcWeb/NuGetCalcWeb
EXPOSE 5000
ENTRYPOINT ["bash", "-c", "mono ../packages/OwinHost.3.0.1/tools/OwinHost.exe -u http://0.0.0.0:5000/"]

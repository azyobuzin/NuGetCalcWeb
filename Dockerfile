FROM azyobuzin/mono:latest
MAINTAINER azyobuzin <azyobuzin@users.sourceforge.jp>

RUN apt-get install -y nodejs npm
RUN ln -s /usr/bin/nodejs /usr/local/bin/node

COPY . /NuGetCalcWeb/

WORKDIR /NuGetCalcWeb
RUN mono .nuget/NuGet.exe restore && xbuild /p:Configuration=Release

WORKDIR /NuGetCalcWeb/NuGetCalcWeb
EXPOSE 5000
ENTRYPOINT ["bash", "-c", "mono ../packages/OwinHost.3.0.1/tools/OwinHost.exe -u http://0.0.0.0:5000/"]

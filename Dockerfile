# Please build this after building binaries

FROM mono:3.12
MAINTAINER azyobuzin <azyobuzin@users.sourceforge.jp>

COPY . /NuGetCalcWeb/

WORKDIR /NuGetCalcWeb/NuGetCalcWeb
EXPOSE 5000
ENTRYPOINT ["bash", "-c", "mono ../packages/OwinHost.3.0.1/tools/OwinHost.exe -u http://0.0.0.0:5000/"]

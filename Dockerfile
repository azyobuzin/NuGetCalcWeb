FROM azyobuzin/mono:eventsourcepatch
MAINTAINER azyobuzin <azyobuzin@users.sourceforge.jp>

RUN git clone https://github.com/azyobuzin/NuGetCalcWeb.git
WORKDIR /NuGetCalcWeb
RUN mono ./.nuget/NuGet.exe restore
RUN xbuild /p:Configuration=Release

WORKDIR /NuGetCalcWeb/NuGetCalcWeb
EXPOSE 5000
ENTRYPOINT ["bash", "-c", "mono ../packages/OwinHost.3.0.1/tools/OwinHost.exe -u http://0.0.0.0:5000/"]

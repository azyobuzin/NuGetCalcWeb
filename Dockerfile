FROM azyobuzin/mono:eventsourcepatch
MAINTAINER azyobuzin <azyobuzin@users.sourceforge.jp>

ADD http://junk.azyobuzi.net/nugetcalcweb/analytics.html http://junk.azyobuzi.net/nugetcalcweb/ad.html /

RUN git clone https://github.com/azyobuzin/NuGetCalcWeb.git
WORKDIR /NuGetCalcWeb
RUN mono ./.nuget/NuGet.exe restore
RUN xbuild /p:Configuration=Release

WORKDIR /NuGetCalcWeb/NuGetCalcWeb
ENV NUGETCALC_ANALYTICS /analytics.html
ENV NUGETCALC_AD /ad.html
EXPOSE 5000
ENTRYPOINT ["bash", "-c", "mono ../packages/OwinHost.3.0.1/tools/OwinHost.exe -u http://0.0.0.0:5000/"]

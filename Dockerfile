# Please build this after building binaries

FROM azyobuzin/mono:eventsourcepatch
MAINTAINER azyobuzin <azyobuzin@users.sourceforge.jp>

ADD http://junk.azyobuzi.net/nugetcalcweb/analytics.html http://junk.azyobuzi.net/nugetcalcweb/ad.html /

COPY . /NuGetCalcWeb/

WORKDIR /NuGetCalcWeb/NuGetCalcWeb
ENV NUGETCALC_ANALYTICS /analytics.html
ENV NUGETCALC_AD /ad.html
EXPOSE 5000
ENTRYPOINT ["bash", "-c", "mono ../packages/OwinHost.3.0.1/tools/OwinHost.exe -u http://0.0.0.0:5000/"]

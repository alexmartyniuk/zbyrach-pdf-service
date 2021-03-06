FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env

WORKDIR /app

COPY ./src/Zbyrach.PdfService/*.csproj ./src/Zbyrach.PdfService/
RUN dotnet restore /app/src/Zbyrach.PdfService/Zbyrach.PdfService.csproj

COPY . ./
RUN dotnet publish /app/src/Zbyrach.PdfService/Zbyrach.PdfService.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1

#####################
#PUPPETEER RECIPE
#####################
# Install latest chrome dev package and fonts to support major charsets (Chinese, Japanese, Arabic, Hebrew, Thai and a few others)
# Note: this installs the necessary libs to make the bundled version of Chromium that Puppeteer
# installs, work.
RUN apt-get update && apt-get -f install && apt-get -y install wget gnupg2 apt-utils
RUN wget -q -O - https://dl-ssl.google.com/linux/linux_signing_key.pub | apt-key add - \
    && sh -c 'echo "deb [arch=amd64] http://dl.google.com/linux/chrome/deb/ stable main" >> /etc/apt/sources.list.d/google.list' \
    && apt-get update \
    && apt-get install -y google-chrome-unstable fonts-ipafont-gothic fonts-wqy-zenhei fonts-thai-tlwg fonts-kacst fonts-freefont-ttf \
      --no-install-recommends \
    && rm -rf /var/lib/apt/lists/*  

ENV PUPPETEER_EXECUTABLE_PATH "/usr/bin/google-chrome-unstable"
#####################
#END PUPPETEER RECIPE
#####################

WORKDIR /app
COPY --from=build-env /app/out .

CMD dotnet Zbyrach.PdfService.dll
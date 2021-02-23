# Zbyrach PDF Service

[![Deploy to Heroku](https://github.com/alexmartyniuk/zbyrach-pdf-service/actions/workflows/deploy-to-heroku.yml/badge.svg)](https://github.com/alexmartyniuk/zbyrach-pdf-service/actions/workflows/deploy-to-heroku.yml)

PDF service is intended to generate PDF file by article URL. It supports 3 different  types of devices:

* mobile 
* tablet
* desktop

PDF Service has own storage for caching PDf files, so next time when you ask about the PDF for the same device type you will get a response much faster.

You can open [Swagger UI](https://zbyrach-pdf-service.herokuapp.com/index.html) and try to make some requests.

* **POST ​/pdf** - generate and return PDF file for provided aritcle. The second time this endpoint will return data from the cache.
* **POST ​/queue** - add article ULR to the queue for processing. If article URL is processed next time you will get the PDF for this article from cache.
* **GET /statistic** - get statistic about the space used by PDF cache.
* **DELETE /cleanup/{days}** - deleted data from the PDF cache for specific datetime range.

![](https://github.com/alexmartyniuk/zbyrach-api/blob/master/docs/img/pdf-service-swagger.png?raw=true)

[Logs server](https://addons-sso.heroku.com/apps/779fd86e-0618-465e-854d-ff252ea1b5fe/addons/6de0a448-4def-4101-9eee-86384f1b50b8)

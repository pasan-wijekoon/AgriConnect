# ZAP Scanning Report

ZAP by [Checkmarx](https://checkmarx.com/).


## Summary of Alerts

| Risk Level | Number of Alerts |
| --- | --- |
| High | 0 |
| Medium | 0 |
| Low | 2 |
| Informational | 3 |




## Insights

| Level | Reason | Site | Description | Statistic |
| --- | --- | --- | --- | --- |
| Info | Informational | http://host.docker.internal:5000 | Percentage of responses with status code 2xx | 20 % |
| Info | Informational | http://host.docker.internal:5000 | Percentage of responses with status code 4xx | 79 % |
| Info | Informational | http://host.docker.internal:5000 | Percentage of endpoints with content type application/json | 20 % |
| Info | Informational | http://host.docker.internal:5000 | Percentage of endpoints with content type application/problem+json | 77 % |
| Info | Informational | http://host.docker.internal:5000 | Percentage of endpoints with method DELETE | 2 % |
| Info | Informational | http://host.docker.internal:5000 | Percentage of endpoints with method GET | 52 % |
| Info | Informational | http://host.docker.internal:5000 | Percentage of endpoints with method PATCH | 9 % |
| Info | Informational | http://host.docker.internal:5000 | Percentage of endpoints with method POST | 25 % |
| Info | Informational | http://host.docker.internal:5000 | Percentage of endpoints with method PUT | 9 % |
| Info | Informational | http://host.docker.internal:5000 | Count of total endpoints | 72    |
| Info | Informational | http://host.docker.internal:5000 | Percentage of slow responses | 5 % |







## Alerts

| Name | Risk Level | Number of Instances |
| --- | --- | --- |
| Cross-Origin-Resource-Policy Header Missing or Invalid | Low | Systemic |
| Timestamp Disclosure - Unix | Low | 2 |
| A Client Error response code was returned by the server | Informational | 57 |
| Authentication Request Identified | Informational | 2 |
| Non-Storable Content | Informational | Systemic |




## Alert Detail



### [ Cross-Origin-Resource-Policy Header Missing or Invalid ](https://www.zaproxy.org/docs/alerts/90004/)



##### Low (Medium)

### Description

Cross-Origin-Resource-Policy header is an opt-in header designed to counter side-channels attacks like Spectre. Resource should be specifically set as shareable amongst different origins.

* URL: http://host.docker.internal:5000/api/Inspections/discrepancies%3FonlyUnresolved=true
  * Node Name: `http://host.docker.internal:5000/api/Inspections/discrepancies (onlyUnresolved)`
  * Method: `GET`
  * Parameter: `Cross-Origin-Resource-Policy`
  * Attack: ``
  * Evidence: ``
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/analytics/filters
  * Node Name: `http://host.docker.internal:5000/api/analytics/filters`
  * Method: `GET`
  * Parameter: `Cross-Origin-Resource-Policy`
  * Attack: ``
  * Evidence: ``
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/auth/me
  * Node Name: `http://host.docker.internal:5000/api/auth/me`
  * Method: `GET`
  * Parameter: `Cross-Origin-Resource-Policy`
  * Attack: ``
  * Evidence: ``
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/collection-centres
  * Node Name: `http://host.docker.internal:5000/api/collection-centres`
  * Method: `GET`
  * Parameter: `Cross-Origin-Resource-Policy`
  * Attack: ``
  * Evidence: ``
  * Other Info: ``
* URL: http://host.docker.internal:5000/swagger/v1/swagger.json
  * Node Name: `http://host.docker.internal:5000/swagger/v1/swagger.json`
  * Method: `GET`
  * Parameter: `Cross-Origin-Resource-Policy`
  * Attack: ``
  * Evidence: ``
  * Other Info: ``

Instances: Systemic


### Solution

Ensure that the application/web server sets the Cross-Origin-Resource-Policy header appropriately, and that it sets the Cross-Origin-Resource-Policy header to 'same-origin' for all web pages.
'same-site' is considered as less secured and should be avoided.
If resources must be shared, set the header to 'cross-origin'.
If possible, ensure that the end user uses a standards-compliant and modern web browser that supports the Cross-Origin-Resource-Policy header (https://caniuse.com/mdn-http_headers_cross-origin-resource-policy).

### Reference


* [ https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Cross-Origin-Embedder-Policy ](https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Cross-Origin-Embedder-Policy)


#### CWE Id: [ 693 ](https://cwe.mitre.org/data/definitions/693.html)


#### WASC Id: 14

#### Source ID: 3

### [ Timestamp Disclosure - Unix ](https://www.zaproxy.org/docs/alerts/10096/)



##### Low (Low)

### Description

A timestamp was disclosed by the application/web server. - Unix

* URL: http://host.docker.internal:5000/api/prices/today%3Fregion=region&grade=A
  * Node Name: `http://host.docker.internal:5000/api/prices/today (grade,region)`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `1544376798`
  * Other Info: `1544376798, which evaluates to: 2018-12-09 17:33:18.`
* URL: http://host.docker.internal:5000/api/prices/today%3Fregion=region&grade=A
  * Node Name: `http://host.docker.internal:5000/api/prices/today (grade,region)`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `1553279768`
  * Other Info: `1553279768, which evaluates to: 2019-03-22 18:36:08.`


Instances: 2

### Solution

Manually confirm that the timestamp data is not sensitive, and that the data cannot be aggregated to disclose exploitable patterns.

### Reference


* [ https://cwe.mitre.org/data/definitions/200.html ](https://cwe.mitre.org/data/definitions/200.html)


#### CWE Id: [ 497 ](https://cwe.mitre.org/data/definitions/497.html)


#### WASC Id: 13

#### Source ID: 3

### [ A Client Error response code was returned by the server ](https://www.zaproxy.org/docs/alerts/100000/)



##### Informational (High)

### Description

A response code of 403 was returned by the server.
This may indicate that the application is failing to handle unexpected input correctly.
Raised by the 'Alert on HTTP Response Code Error' script

* URL: http://host.docker.internal:5000/api/admin/today-prices-catalog/id
  * Node Name: `http://host.docker.internal:5000/api/admin/today-prices-catalog/id`
  * Method: `DELETE`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/listings/id
  * Node Name: `http://host.docker.internal:5000/api/listings/id`
  * Method: `DELETE`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/Inspections%3FSearch=ZAP&Grade=Grade&CropId=CropId&RegionId=RegionId&FromDate=FromDate&ToDate=ToDate&Page=10&PageSize=10
  * Node Name: `http://host.docker.internal:5000/api/Inspections (CropId,FromDate,Grade,Page,PageSize,RegionId,Search,ToDate)`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/Inspections/id
  * Node Name: `http://host.docker.internal:5000/api/Inspections/id`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/admin/today-prices-catalog
  * Node Name: `http://host.docker.internal:5000/api/admin/today-prices-catalog`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/admin/users%3Fsearch=ZAP&role=role&page=1&size=20
  * Node Name: `http://host.docker.internal:5000/api/admin/users (page,role,search,size)`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/analytics/anomalies%3Fstatus=status&cropId=cropId&page=1&size=20
  * Node Name: `http://host.docker.internal:5000/api/analytics/anomalies (cropId,page,size,status)`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/analytics/anomalies/listingId/investigate
  * Node Name: `http://host.docker.internal:5000/api/analytics/anomalies/listingId/investigate`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/analytics/price-trends%3FcropId=cropId&regionId=regionId&from=from&to=to&bucket=week
  * Node Name: `http://host.docker.internal:5000/api/analytics/price-trends (bucket,cropId,from,regionId,to)`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/analytics/shortages%3FcropId=cropId&regionId=regionId&type=type&severity=severity
  * Node Name: `http://host.docker.internal:5000/api/analytics/shortages (cropId,regionId,severity,type)`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/collection-centres/centreId/schedules
  * Node Name: `http://host.docker.internal:5000/api/collection-centres/centreId/schedules`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/collection-centres/nearest%3Flat=1.2&lng=1.2&regionId=regionId
  * Node Name: `http://host.docker.internal:5000/api/collection-centres/nearest (lat,lng,regionId)`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/listings%3FCropId=CropId&RegionId=RegionId&ExcludeFarmerId=ExcludeFarmerId&Status=Status&Grade=Grade&MinPrice=1.2&MaxPrice=1.2&Search=ZAP&SortBy=SortBy&SortDir=SortDir&Page=10&PageSize=10
  * Node Name: `http://host.docker.internal:5000/api/listings (CropId,ExcludeFarmerId,Grade,MaxPrice,MinPrice,Page,PageSize,RegionId,Search,SortBy,SortDir,Status)`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/listings/id
  * Node Name: `http://host.docker.internal:5000/api/listings/id`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/listings/id/agent-workflow
  * Node Name: `http://host.docker.internal:5000/api/listings/id/agent-workflow`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/listings/id/inspections
  * Node Name: `http://host.docker.internal:5000/api/listings/id/inspections`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/listings/id/price-suggestion
  * Node Name: `http://host.docker.internal:5000/api/listings/id/price-suggestion`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/listings/my%3FCropId=CropId&RegionId=RegionId&ExcludeFarmerId=ExcludeFarmerId&Status=Status&Grade=Grade&MinPrice=1.2&MaxPrice=1.2&Search=ZAP&SortBy=SortBy&SortDir=SortDir&Page=10&PageSize=10
  * Node Name: `http://host.docker.internal:5000/api/listings/my (CropId,ExcludeFarmerId,Grade,MaxPrice,MinPrice,Page,PageSize,RegionId,Search,SortBy,SortDir,Status)`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/listings/my-listings
  * Node Name: `http://host.docker.internal:5000/api/listings/my-listings`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/orders/id
  * Node Name: `http://host.docker.internal:5000/api/orders/id`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/orders/id/activity
  * Node Name: `http://host.docker.internal:5000/api/orders/id/activity`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/orders/id/schedule
  * Node Name: `http://host.docker.internal:5000/api/orders/id/schedule`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/orders/id/schedule/alternatives
  * Node Name: `http://host.docker.internal:5000/api/orders/id/schedule/alternatives`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/reports%3Fpage=1&size=20
  * Node Name: `http://host.docker.internal:5000/api/reports (page,size)`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/reports/id
  * Node Name: `http://host.docker.internal:5000/api/reports/id`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5000/uploads/fileName
  * Node Name: `http://host.docker.internal:5000/uploads/fileName`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/admin/users/id/role
  * Node Name: `http://host.docker.internal:5000/api/admin/users/id/role ()({role,collectionCentreId})`
  * Method: `PATCH`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/admin/users/id/status
  * Node Name: `http://host.docker.internal:5000/api/admin/users/id/status ()({isActive})`
  * Method: `PATCH`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/analytics/anomalies/id
  * Node Name: `http://host.docker.internal:5000/api/analytics/anomalies/id ()({status})`
  * Method: `PATCH`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/listings/id/price-suggestion/approve
  * Node Name: `http://host.docker.internal:5000/api/listings/id/price-suggestion/approve`
  * Method: `PATCH`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/listings/id/price-suggestion/reject
  * Node Name: `http://host.docker.internal:5000/api/listings/id/price-suggestion/reject ()({officerNote})`
  * Method: `PATCH`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/listings/id/price-suggestion/revise
  * Node Name: `http://host.docker.internal:5000/api/listings/id/price-suggestion/revise ()({revisedPriceMin,revisedPriceMax,officerNote})`
  * Method: `PATCH`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/listings/id/reject
  * Node Name: `http://host.docker.internal:5000/api/listings/id/reject`
  * Method: `PATCH`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/Inspections
  * Node Name: `http://host.docker.internal:5000/api/Inspections ()({listingId,confirmedGrade,notes,photoUrls:[]})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/Inspections/discrepancies/id/resolve
  * Node Name: `http://host.docker.internal:5000/api/Inspections/discrepancies/id/resolve ()({resolutionNotes})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/Upload
  * Node Name: `http://host.docker.internal:5000/api/Upload ()(multipart:file)`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/admin/today-prices-catalog
  * Node Name: `http://host.docker.internal:5000/api/admin/today-prices-catalog ()({name,category,unit,defaultRegion,imageUrl,displayOrder})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/admin/users
  * Node Name: `http://host.docker.internal:5000/api/admin/users ()({fullName,email,password,role,phone,region,collectionCentreId})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/admin/users/id/reset-credentials
  * Node Name: `http://host.docker.internal:5000/api/admin/users/id/reset-credentials ()({newPassword})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/analytics/scheduling-preview
  * Node Name: `http://host.docker.internal:5000/api/analytics/scheduling-preview ()({centreId,preferredWindow:{start,end},existingBookings:[{slotStart,slotEnd}]})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/analytics/snapshots/refresh
  * Node Name: `http://host.docker.internal:5000/api/analytics/snapshots/refresh`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/auth/login
  * Node Name: `http://host.docker.internal:5000/api/auth/login ()({email,password})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/auth/register
  * Node Name: `http://host.docker.internal:5000/api/auth/register ()({fullName,email,password,role,phone,region})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/listings
  * Node Name: `http://host.docker.internal:5000/api/listings ()({cropId,regionId,quantity,unit,claimedGrade,pickupWindowStart,pickupWindowEnd,minPrice,description,photoUrls:[]})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/listings/id/evaluate-compliance
  * Node Name: `http://host.docker.internal:5000/api/listings/id/evaluate-compliance`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/listings/id/photos
  * Node Name: `http://host.docker.internal:5000/api/listings/id/photos ()({photoUrls:[]})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/listings/id/publish
  * Node Name: `http://host.docker.internal:5000/api/listings/id/publish`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/orders
  * Node Name: `http://host.docker.internal:5000/api/orders ()({listingId,quantity,deliveryPreference,buyerLat,buyerLng})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/orders/id/cancel
  * Node Name: `http://host.docker.internal:5000/api/orders/id/cancel ()({reason})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/orders/id/schedule
  * Node Name: `http://host.docker.internal:5000/api/orders/id/schedule ()({collectionCentreId,preferredWindow:{start,end},buyerLocation:{lat,lng}})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/reports/export
  * Node Name: `http://host.docker.internal:5000/api/reports/export ()({type,dateRangeStart,dateRangeEnd})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/Inspections/id
  * Node Name: `http://host.docker.internal:5000/api/Inspections/id ()({confirmedGrade,notes,photoUrls:[],reasonForAmendment})`
  * Method: `PUT`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/admin/today-prices-catalog/id
  * Node Name: `http://host.docker.internal:5000/api/admin/today-prices-catalog/id ()({name,category,unit,defaultRegion,imageUrl,displayOrder,isActive})`
  * Method: `PUT`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/listings/id
  * Node Name: `http://host.docker.internal:5000/api/listings/id ()({cropId,regionId,quantity,unit,claimedGrade,pickupWindowStart,pickupWindowEnd,minPrice,description,photoUrls:[]})`
  * Method: `PUT`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/notifications/id/read
  * Node Name: `http://host.docker.internal:5000/api/notifications/id/read`
  * Method: `PUT`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/orders/id/schedule/decision
  * Node Name: `http://host.docker.internal:5000/api/orders/id/schedule/decision ()({decision,reason,preferredWindow:{start,end}})`
  * Method: `PUT`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/orders/id/status
  * Node Name: `http://host.docker.internal:5000/api/orders/id/status ()({status})`
  * Method: `PUT`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``


Instances: 57

### Solution



### Reference



#### CWE Id: [ 388 ](https://cwe.mitre.org/data/definitions/388.html)


#### WASC Id: 20

#### Source ID: 4

### [ Authentication Request Identified ](https://www.zaproxy.org/docs/alerts/10111/)



##### Informational (Low)

### Description

The given request has been identified as an authentication request. The 'Other Info' field contains a set of key=value lines which identify any relevant fields. If the request is in a context which has an Authentication Method set to "Auto-Detect" then this rule will change the authentication to match the request identified.

* URL: http://host.docker.internal:5000/api/admin/users
  * Node Name: `http://host.docker.internal:5000/api/admin/users ()({fullName,email,password,role,phone,region,collectionCentreId})`
  * Method: `POST`
  * Parameter: `email`
  * Attack: ``
  * Evidence: `password`
  * Other Info: `userParam=email
userValue=zaproxy@example.com
passwordParam=password`
* URL: http://host.docker.internal:5000/api/auth/login
  * Node Name: `http://host.docker.internal:5000/api/auth/login ()({email,password})`
  * Method: `POST`
  * Parameter: `email`
  * Attack: ``
  * Evidence: `password`
  * Other Info: `userParam=email
userValue=zaproxy@example.com
passwordParam=password`


Instances: 2

### Solution

This is an informational alert rather than a vulnerability and so there is nothing to fix.

### Reference


* [ https://www.zaproxy.org/docs/desktop/addons/authentication-helper/auth-req-id/ ](https://www.zaproxy.org/docs/desktop/addons/authentication-helper/auth-req-id/)



#### Source ID: 3

### [ Non-Storable Content ](https://www.zaproxy.org/docs/alerts/10049/)



##### Informational (Medium)

### Description

The response contents are not storable by caching components such as proxy servers. If the response does not contain sensitive, personal or user-specific information, it may benefit from being stored and cached, to improve performance.

* URL: http://host.docker.internal:5000/api/analytics/anomalies%3Fstatus=status&cropId=cropId&page=1&size=20
  * Node Name: `http://host.docker.internal:5000/api/analytics/anomalies (cropId,page,size,status)`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `authorization:`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/analytics/price-trends%3FcropId=cropId&regionId=regionId&from=from&to=to&bucket=week
  * Node Name: `http://host.docker.internal:5000/api/analytics/price-trends (bucket,cropId,from,regionId,to)`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `authorization:`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/admin/users/id/role
  * Node Name: `http://host.docker.internal:5000/api/admin/users/id/role ()({role,collectionCentreId})`
  * Method: `PATCH`
  * Parameter: ``
  * Attack: ``
  * Evidence: `PATCH `
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/admin/users
  * Node Name: `http://host.docker.internal:5000/api/admin/users ()({fullName,email,password,role,phone,region,collectionCentreId})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `authorization:`
  * Other Info: ``
* URL: http://host.docker.internal:5000/api/admin/users/id/reset-credentials
  * Node Name: `http://host.docker.internal:5000/api/admin/users/id/reset-credentials ()({newPassword})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `authorization:`
  * Other Info: ``

Instances: Systemic


### Solution

The content may be marked as storable by ensuring that the following conditions are satisfied:
The request method must be understood by the cache and defined as being cacheable ("GET", "HEAD", and "POST" are currently defined as cacheable)
The response status code must be understood by the cache (one of the 1XX, 2XX, 3XX, 4XX, or 5XX response classes are generally understood)
The "no-store" cache directive must not appear in the request or response header fields
For caching by "shared" caches such as "proxy" caches, the "private" response directive must not appear in the response
For caching by "shared" caches such as "proxy" caches, the "Authorization" header field must not appear in the request, unless the response explicitly allows it (using one of the "must-revalidate", "public", or "s-maxage" Cache-Control response directives)
In addition to the conditions above, at least one of the following conditions must also be satisfied by the response:
It must contain an "Expires" header field
It must contain a "max-age" response directive
For "shared" caches such as "proxy" caches, it must contain a "s-maxage" response directive
It must contain a "Cache Control Extension" that allows it to be cached
It must have a status code that is defined as cacheable by default (200, 203, 204, 206, 300, 301, 404, 405, 410, 414, 501).

### Reference


* [ https://datatracker.ietf.org/doc/html/rfc7234 ](https://datatracker.ietf.org/doc/html/rfc7234)
* [ https://datatracker.ietf.org/doc/html/rfc7231 ](https://datatracker.ietf.org/doc/html/rfc7231)
* [ https://www.w3.org/Protocols/rfc2616/rfc2616-sec13.html ](https://www.w3.org/Protocols/rfc2616/rfc2616-sec13.html)


#### CWE Id: [ 524 ](https://cwe.mitre.org/data/definitions/524.html)


#### WASC Id: 13

#### Source ID: 3



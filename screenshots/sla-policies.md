#### **Get Policies**

![get-sla-policies page Screenshot](screenshots/get-sla-policies.png)

Request URL: https://supportdeskpro-api.victoriousdune-73ebad30.westus.azurecontainerapps.io/api/sla-policies?page=1&pageSize=20

Request Method: GET

Response:
```
{
    "success": true,
    "data": {
        "items": [
            {
                "id": "73b0359c-38d2-4278-88cb-6df8b3ad6834",
                "name": "Critical SLA",
                "priority": "Critical",
                "firstResponseTimeMinutes": 60,
                "resolutionTimeMinutes": 240,
                "isActive": true,
                "createdAt": "2026-04-06T18:03:13.6439569"
            }
        ],
        "totalCount": 1,
        "page": 1,
        "pageSize": 20
    },
    "message": null,
    "errors": null,
    "pagination": null
}
```

---

#### **Edit SLA Policy**

![edit-sla-policies page Screenshot](screenshots/edit-sla-policies.png)

Request URL: https://supportdeskpro-api.victoriousdune-73ebad30.westus.azurecontainerapps.io/api/sla-policies/73b0359c-38d2-4278-88cb-6df8b3ad6834

Request Method: PUT

Request Payload:
```
{
    "name": "Critical SLA",
    "firstResponseTimeMinutes": 60,
    "resolutionTimeMinutes": 240
}
```

Response:
```
{
    "success": true,
    "data": "SLA policy updated successfully.",
    "message": null,
    "errors": null,
    "pagination": null
}
```

---

#### **Deactivate SLA Policy**

![deactivate-sla-policy page Screenshot](screenshots/deactivate-sla-policy.png)

Request URL: https://supportdeskpro-api.victoriousdune-73ebad30.westus.azurecontainerapps.io/api/sla-policies/73b0359c-38d2-4278-88cb-6df8b3ad6834/status

Request Method: PATCH

Response:
```
{
    "success": true,
    "data": "SLA policy deactivated successfully.",
    "message": null,
    "errors": null,
    "pagination": null
}
```

---

#### **Get Active Policies**

![get-active-sla-policies page Screenshot](screenshots/get-active-sla-policies.png)

Request URL: https://supportdeskpro-api.victoriousdune-73ebad30.westus.azurecontainerapps.io/api/sla-policies?page=1&pageSize=20&isActive=true

Request Method: GET

Response:
```
{
    "success": true,
    "data": {
        "items": [
            {
                "id": "42e7d574-3fdb-48e1-80e3-b099b0290f18",
                "name": "Standard SLA",
                "priority": "Low",
                "firstResponseTimeMinutes": 1440,
                "resolutionTimeMinutes": 4320,
                "isActive": true,
                "createdAt": "2026-04-06T18:14:32.5229913"
            },
            {
                "id": "98ce2178-5b2c-4525-9a66-8043c497e0f2",
                "name": "Medium Priority SLA",
                "priority": "Medium",
                "firstResponseTimeMinutes": 480,
                "resolutionTimeMinutes": 2880,
                "isActive": true,
                "createdAt": "2026-04-06T18:13:57.0471562"
            }
        ]
    },
    "message": null,
    "errors": null,
    "pagination": null
}
```

---

#### **Get Inactive Policies**

![get-inactive-sla-policies page Screenshot](screenshots/get-inactive-sla-policies.png)

Request URL: https://supportdeskpro-api.victoriousdune-73ebad30.westus.azurecontainerapps.io/api/sla-policies?page=1&pageSize=20&isActive=false

Request Method: GET

Response:
```
{
    "success": true,
    "data": {
        "items": [
            {
                "id": "73b0359c-38d2-4278-88cb-6df8b3ad6834",
                "name": "Critical SLA",
                "priority": "Critical",
                "firstResponseTimeMinutes": 60,
                "resolutionTimeMinutes": 240,
                "isActive": false,
                "createdAt": "2026-04-06T18:03:13.6439569"
            }
        ],
        "totalCount": 1,
        "page": 1,
        "pageSize": 20
    },
    "message": null,
    "errors": null,
    "pagination": null
}
```

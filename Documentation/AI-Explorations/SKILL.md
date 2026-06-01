# DocumentHub — AI Agent Skill

## Overview

**DocumentHub** is a DataMiner solution for centralized document and file management. It supports multiple storage backends (local DataMiner file system, SharePoint Online, and DOM attachments) and organizes files into configurable **Document Buckets**. Use this skill when you need to list, upload, or delete files stored in DataMiner, or when you need to query the storage configuration (buckets, DOM sources, SharePoint setups).

This skill is exposed as a **DataMiner User-Defined API** and requires a valid API token.

---

## Base URL

```
https://{dataminer-hostname}/api/custom/documenthub/
```

---

## Authentication

All requests must include a Bearer token in the `Authorization` header:

```http
Authorization: Bearer <your-api-token>
```

API tokens are created and managed in DataMiner Cube under **System Center → User-Defined APIs**.

> **Note:** The token secret is only visible at creation time. Store it securely.

---

## Capabilities

| Method   | Route                                    | Description                                           |
|----------|------------------------------------------|-------------------------------------------------------|
| `GET`    | `documenthub/buckets`                    | List all configured Document Buckets                  |
| `GET`    | `documenthub/buckets/{name}/files`       | List files in a specific bucket                       |
| `POST`   | `documenthub/buckets/{name}/files`       | Upload a file to a bucket                             |
| `DELETE` | `documenthub/buckets/{name}/files`       | Delete a file from a bucket                           |
| `GET`    | `documenthub/sources`                    | List all DOM Sources                                  |
| `GET`    | `documenthub/sharepoint`                 | List all SharePoint Configurations                    |

---

## Endpoints

### GET `documenthub/buckets`

Returns all configured Document Buckets.

**Query parameters:** none

**Example:**
```bash
curl -X GET "https://my-dma.example.com/api/custom/documenthub/buckets" \
  -H "Authorization: Bearer <token>"
```

**Response `200 OK`:**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Reports",
    "description": "Monthly reports",
    "uploadPath": "/reports",
    "storageType": "Local",
    "extensions": "pdf,docx,xlsx",
    "isDefault": false,
    "sizeLimit": 10485760
  }
]
```

---

### GET `documenthub/buckets/{name}/files`

Lists files stored in the bucket identified by `{name}`.

**Query parameters:**

| Parameter | Type   | Required | Description                                  |
|-----------|--------|----------|----------------------------------------------|
| `filter`  | string | No       | Case-insensitive substring filter on filename |

**Example:**
```bash
curl -X GET "https://my-dma.example.com/api/custom/documenthub/buckets/Reports/files?filter=monthly" \
  -H "Authorization: Bearer <token>"
```

**Response `200 OK`:**
```json
[
  {
    "name": "monthly_report.pdf",
    "path": "/reports/monthly_report.pdf",
    "storageType": "Local"
  }
]
```

**Response `404 Not Found`:** Bucket with the given name does not exist.

---

### POST `documenthub/buckets/{name}/files`

Uploads a file to the bucket identified by `{name}`.

**Request body (JSON):**

| Field               | Type   | Required | Description                                           |
|---------------------|--------|----------|-------------------------------------------------------|
| `filePath`          | string | Yes      | Full local path of the file on the DataMiner Agent    |
| `customName`        | string | No       | Custom file name without extension                    |
| `uploadPathQualifier` | string | No    | Additional subfolder path appended to the bucket path |
| `domInstanceId`     | string | No       | GUID of a DOM instance to link the file to            |

**Example:**
```bash
curl -X POST "https://my-dma.example.com/api/custom/documenthub/buckets/Reports/files" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "filePath": "C:\\Temp\\Q1_Report.pdf",
    "customName": "q1_report",
    "uploadPathQualifier": "2024/Q1"
  }'
```

**Response `201 Created`:**
```json
{
  "uploadedPath": "/reports/2024/Q1/q1_report.pdf"
}
```

**Response `400 Bad Request`:** Missing or invalid parameters.  
**Response `404 Not Found`:** Bucket not found.  
**Response `500 Internal Server Error`:** File already exists or upload failed.

---

### DELETE `documenthub/buckets/{name}/files`

Deletes a file from the bucket identified by `{name}`.

> Currently supported for `Local` storage only.

**Query parameters:**

| Parameter  | Type   | Required | Description                              |
|------------|--------|----------|------------------------------------------|
| `fileName` | string | Yes      | Name of the file to delete (with extension) |

**Example:**
```bash
curl -X DELETE "https://my-dma.example.com/api/custom/documenthub/buckets/Reports/files?fileName=q1_report.pdf" \
  -H "Authorization: Bearer <token>"
```

**Response `204 No Content`:** File successfully deleted.  
**Response `400 Bad Request`:** `fileName` parameter missing.  
**Response `404 Not Found`:** Bucket or file not found.

---

### GET `documenthub/sources`

Returns all configured DOM Sources.

**Example:**
```bash
curl -X GET "https://my-dma.example.com/api/custom/documenthub/sources" \
  -H "Authorization: Bearer <token>"
```

**Response `200 OK`:**
```json
[
  {
    "id": "7cb46b0e-1a2c-4d3e-9f1a-000000000001",
    "name": "My DOM Source",
    "module": "my_dom_module"
  }
]
```

---

### GET `documenthub/sharepoint`

Returns all configured SharePoint configurations.

> **Note:** `clientSecret` is never returned in the response.

**Example:**
```bash
curl -X GET "https://my-dma.example.com/api/custom/documenthub/sharepoint" \
  -H "Authorization: Bearer <token>"
```

**Response `200 OK`:**
```json
[
  {
    "id": "1a2b3c4d-0000-0000-0000-000000000001",
    "tenantId": "your-tenant-id",
    "clientId": "your-client-id",
    "siteUrl": "https://contoso.sharepoint.com/sites/MySite",
    "documentLibraryName": "Shared Documents",
    "status": "Active"
  }
]
```

---

## Response Codes

| Code  | Meaning              | When it occurs                                              |
|-------|----------------------|-------------------------------------------------------------|
| `200` | OK                   | Successful read operation                                   |
| `201` | Created              | File successfully uploaded                                  |
| `204` | No Content           | File successfully deleted                                   |
| `400` | Bad Request          | Missing required parameters or invalid input                |
| `404` | Not Found            | Bucket, file, or resource not found                         |
| `405` | Method Not Allowed   | HTTP method not supported on this route                     |
| `500` | Internal Server Error| Unexpected error, file already exists, or upload failure    |

---

## Storage Types

| Value        | Description                                                   |
|--------------|---------------------------------------------------------------|
| `Local`      | Files stored on the DataMiner Agent's file system             |
| `SharePoint` | Files stored in a SharePoint Online document library          |
| `DOM`        | Files attached to DataMiner DOM instances                     |

---

## Limitations & Notes

- Requires **DataMiner 10.3.6+** with the `UserDefinableApiEndpoint` DxM installed
- Requires an **indexing database** (Elasticsearch, OpenSearch, or STaaS)
- Maximum response body size: **29 MB**
- API tokens are stored in the indexing database and are **not included in DataMiner backups** — back them up separately
- File deletion is currently supported for `Local` storage only
- Routes are **case-insensitive**
- Token secrets **cannot be retrieved** after creation

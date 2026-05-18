---
title:
  en: Telemetry backend
  zh-CN: 遥测后端
---

## Scope{lang="en"}

::: en

RitsuLib only handles consent, local queueing, routing, and payload assembly. Each applicant owns its fixed endpoint. A backend may be a Cloudflare Worker, FastAPI service, ASP.NET service, PostHog proxy, S3 writer, or any other service that accepts the same batch contract.

The public contract lives in:

- `schemas/telemetry/v1/openapi.yaml`
- `schemas/telemetry/v1/telemetry-batch.schema.json`
- `schemas/telemetry/v1/telemetry-event.schema.json`

Use the OpenAPI file with tools such as OpenAPI Generator, Kiota, NSwag, or Swagger Codegen. Use the JSON Schema files for runtime validation in workers, FastAPI, ASP.NET, Node, Rust, Go, or Java.

:::

## 范围{lang="zh-CN"}

::: zh-CN

RitsuLib 只负责授权、本地队列、路由和 payload 组装。每个申请方拥有自己的固定端点。后端可以是 Cloudflare Worker、FastAPI、ASP.NET、PostHog 转发器、S3 写入器，或任何接受同一 batch contract 的服务。

公共契约文件位于：

- `schemas/telemetry/v1/openapi.yaml`
- `schemas/telemetry/v1/telemetry-batch.schema.json`
- `schemas/telemetry/v1/telemetry-event.schema.json`

`openapi.yaml` 可用于 OpenAPI Generator、Kiota、NSwag、Swagger Codegen 等工具生成代码。JSON Schema 可用于 Worker、FastAPI、ASP.NET、Node、Rust、Go、Java 等运行时校验。

:::

## Endpoint{lang="en"}

::: en

The recommended endpoint is:

```text
POST /v1/ingest
Content-Type: application/json
```

Successful responses should return `200` or `202`:

```json
{
  "ok": true,
  "accepted": 2,
  "rejected": 0,
  "request_id": "optional-log-correlation-id"
}
```

Error responses should use a stable machine-readable `error` string:

```json
{
  "error": "invalid_schema",
  "message": "schema must be ritsulib.telemetry.batch.v1"
}
```

:::

## 端点{lang="zh-CN"}

::: zh-CN

推荐端点：

```text
POST /v1/ingest
Content-Type: application/json
```

成功响应建议返回 `200` 或 `202`：

```json
{
  "ok": true,
  "accepted": 2,
  "rejected": 0,
  "request_id": "optional-log-correlation-id"
}
```

错误响应应使用稳定的机器可读 `error` 字符串：

```json
{
  "error": "invalid_schema",
  "message": "schema must be ritsulib.telemetry.batch.v1"
}
```

:::

## Payload{lang="en"}

::: en

A batch has a batch schema id, one applicant id, and one or more events:

```json
{
  "schema": "ritsulib.telemetry.batch.v1",
  "applicant_id": "author.some-mod",
  "events": [
    {
      "schema": "ritsulib.telemetry.v1",
      "applicantId": "author.some-mod",
      "eventName": "exception",
      "requestId": "diagnostics",
      "category": "Diagnostics",
      "timestampUtc": "2026-05-19T00:00:00Z",
      "properties": {
        "anonymous_install_id": "stable-anonymous-id",
        "session_id": "process-session-id",
        "ritsulib_version": "0.0.0",
        "applicant_id": "author.some-mod",
        "owner_mod_id": "author.some-mod",
        "payload_kind": "exception",
        "exception_type": "System.Exception"
      },
      "payload": {
        "applicant_payload": {
          "exception": {
            "type": "System.Exception",
            "message": "example",
            "stack_trace": "..."
          }
        }
      }
    }
  ]
}
```

Backends should index `properties` first. Full `payload` should be stored as JSON/blob. Promote only the fields needed for dashboards or search.

:::

## 数据{lang="zh-CN"}

::: zh-CN

一个 batch 包含 batch schema id、一个申请方 ID，以及一个或多个事件：

```json
{
  "schema": "ritsulib.telemetry.batch.v1",
  "applicant_id": "author.some-mod",
  "events": [
    {
      "schema": "ritsulib.telemetry.v1",
      "applicantId": "author.some-mod",
      "eventName": "exception",
      "requestId": "diagnostics",
      "category": "Diagnostics",
      "timestampUtc": "2026-05-19T00:00:00Z",
      "properties": {
        "anonymous_install_id": "stable-anonymous-id",
        "session_id": "process-session-id",
        "ritsulib_version": "0.0.0",
        "applicant_id": "author.some-mod",
        "owner_mod_id": "author.some-mod",
        "payload_kind": "exception",
        "exception_type": "System.Exception"
      },
      "payload": {
        "applicant_payload": {
          "exception": {
            "type": "System.Exception",
            "message": "example",
            "stack_trace": "..."
          }
        }
      }
    }
  ]
}
```

后端应优先索引 `properties`。完整 `payload` 建议按 JSON/blob 存储，只把看板或搜索所需字段提升为索引字段。

:::

## Backend Checklist{lang="en"}

::: en

- Validate `schema` and `event.schema`.
- Validate `applicant_id` and every `event.applicantId` against the endpoint owner.
- Enforce request body size and event count limits.
- Reject or quarantine unknown schema versions instead of silently reshaping them.
- Store raw events before forwarding to analytics if durability matters.
- Use server-side secrets for analytics keys. Do not embed PostHog or warehouse write keys in mods.
- Keep an append-only raw table or object store for later reprocessing.
- Promote query-critical fields from `properties` and selected payload paths into indexed columns.

:::

## 后端检查项{lang="zh-CN"}

::: zh-CN

- 校验 `schema` 和 `event.schema`。
- 校验 `applicant_id` 与每个 `event.applicantId` 是否属于该端点所有者。
- 限制请求体大小和事件数量。
- 对未知 schema 版本应拒绝或隔离，不要静默改写。
- 如果需要可靠性，先持久化 raw event，再转发到分析平台。
- 分析平台 key 必须保存在服务端，不要写进 mod。
- 保留 append-only raw table 或对象存储，方便以后重放处理。
- 将 `properties` 和少量重要 payload 路径提升为索引字段。

:::

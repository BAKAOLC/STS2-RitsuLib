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

RitsuLib 只负责授权、本地排队、路由和事件数据组装。每个申请方使用自己的固定接收端。后端可以是 Cloudflare Worker、FastAPI、ASP.NET、PostHog 转发器、S3 写入服务，或其他接受同一批次契约的服务。

公共契约文件位于：

- `schemas/telemetry/v1/openapi.yaml`
- `schemas/telemetry/v1/telemetry-batch.schema.json`
- `schemas/telemetry/v1/telemetry-event.schema.json`

`openapi.yaml` 可供 OpenAPI Generator、Kiota、NSwag、Swagger Codegen 等工具生成代码。JSON Schema 可用于 Worker、FastAPI、ASP.NET、Node、Rust、Go、Java 等环境中的运行时校验。

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

## 接收端{lang="zh-CN"}

::: zh-CN

推荐的接收端为：

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

`payload` may contain `base_payload`, `private_contributions`, `shared_contributions`, and `applicant_payload`. Private contributions are data supplied by the applicant's own mod. Shared contributions are data from another mod source and are only included after explicit source consent.

:::

## 数据{lang="zh-CN"}

::: zh-CN

一个批次包含批次架构 ID、一个申请方 ID，以及一个或多个事件：

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

后端应优先索引 `properties`。完整的 `payload` 建议以 JSON 或二进制大对象保存，只将看板和搜索所需的字段提升为索引字段。

`payload` 可能包含 `base_payload`、`private_contributions`、`shared_contributions` 和 `applicant_payload`。私有附加数据来自申请方自己的模组；共享附加数据来自其他模组，并且只有在用户单独授权该来源后才会包含。

:::

## Backend Checklist{lang="en"}

::: en

- Validate `schema` and `event.schema`.
- Validate `applicant_id` and every `event.applicantId` against the endpoint owner.
- Enforce request body size and event count limits.
- Reject or quarantine unknown schema versions instead of silently reshaping them.
- Store raw events before forwarding to analytics if durability matters.
- Keep personal, secret, and warehouse write keys on the server. A PostHog project token used by its public
  capture endpoint is not a substitute for those private credentials.
- Keep an append-only raw table or object store for later reprocessing.
- Promote query-critical fields from `properties` and selected payload paths into indexed columns.

:::

## 后端检查项{lang="zh-CN"}

::: zh-CN

- 校验 `schema` 和 `event.schema`。
- 校验 `applicant_id` 与每个 `event.applicantId` 是否属于该端点所有者。
- 限制请求体大小和事件数量。
- 对未知架构版本应拒绝或隔离，不要静默改写。
- 如果需要保证持久性，应先保存原始事件，再转发至分析平台。
- 个人密钥、秘密密钥和数据仓库写入密钥必须保存在服务端。PostHog 公共采集接口使用的项目令牌不能代替这些私密凭据。
- 保留仅追加的原始数据表或对象存储，以便日后重新处理。
- 将 `properties` 和少量重要的 `payload` 路径提升为索引字段。

:::

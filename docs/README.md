# RitsuLib documentation site

Valaxy + `valaxy-theme-nova`, modeled after the DevMode docs layout.

This project lives in the repository’s **`docs/`** directory. Handbook-style Markdown for guides is under [`pages/guide/`](pages/guide/). Bilingual pages follow Valaxy’s pattern: `## Heading{lang="en"}` / `## …{lang="zh-CN"}`, with `::: en` / `::: zh-CN` blocks (see [Valaxy i18n](https://valaxy.site/guide/i18n)).

Local preview:

```bash
cd docs
pnpm install
pnpm dev
```

Production build:

```bash
pnpm build
```

# React + TypeScript + Vite

This template provides a minimal setup to get React working in Vite with HMR and some Oxlint rules.

Currently, two official plugins are available:

- [@vitejs/plugin-react](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react) uses [Oxc](https://oxc.rs)
- [@vitejs/plugin-react-swc](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react-swc) uses [SWC](https://swc.rs/)

## React Compiler

The React Compiler is not enabled on this template because of its impact on dev & build performances. To add it, see [this documentation](https://react.dev/learn/react-compiler/installation).

## Expanding the Oxlint configuration

If you are developing a production application, we recommend enabling type-aware lint rules by installing `oxlint-tsgolint` and editing `.oxlintrc.json`:

```json
{
  "$schema": "./node_modules/oxlint/configuration_schema.json",
  "plugins": ["react", "typescript", "oxc"],
  "options": {
    "typeAware": true
  },
  "rules": {
    "react/rules-of-hooks": "error",
    "react/only-export-components": ["warn", { "allowConstantExport": true }]
  }
}
```

See the [Oxlint rules documentation](https://oxc.rs/docs/guide/usage/linter/rules) for the full list of rules and categories.

## Testing

Unit and component tests use [Vitest](https://vitest.dev/) and [Testing Library](https://testing-library.com/react).

```bash
npm test
npm run test:watch
```

Put new tests under `src/__tests__/` as `*.test.tsx` or `*.spec.tsx` files.

## Leo chat

The agents module now includes a chat page for `Leo` at `/module/agents/leo`.

- By default the frontend calls `/webhook/leo-think`.
- In local development Vite proxies `/webhook/*` to `https://n8n.neberg.de`.
- If a different endpoint is needed, set `VITE_LEO_WEBHOOK_URL` before starting Vite.

Example:

```bash
VITE_LEO_WEBHOOK_URL=https://example.test/webhook/leo-think npm run dev
```


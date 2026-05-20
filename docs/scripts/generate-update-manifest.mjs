import { mkdir, readFile, writeFile } from 'node:fs/promises'
import { dirname, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

const scriptDir = dirname(fileURLToPath(import.meta.url))
const docsRoot = resolve(scriptDir, '..')
const repoRoot = resolve(docsRoot, '..')

const templatePath = resolve(docsRoot, 'ritsulib-update.template.json')
const modManifestPath = resolve(repoRoot, 'mod_manifest.json')
const outputPath = resolve(docsRoot, 'public', 'ritsulib-update.json')

const [templateText, modManifestText] = await Promise.all([
  readFile(templatePath, 'utf8'),
  readFile(modManifestPath, 'utf8'),
])

const template = JSON.parse(templateText)
const modManifest = JSON.parse(modManifestText)

if (typeof modManifest.version !== 'string' || modManifest.version.trim().length === 0) {
  throw new Error('mod_manifest.json must contain a non-empty version string.')
}

const output = {
  ...template,
  latest_version: modManifest.version.trim(),
}

await mkdir(dirname(outputPath), { recursive: true })
await writeFile(outputPath, `${JSON.stringify(output, null, 2)}\n`, 'utf8')


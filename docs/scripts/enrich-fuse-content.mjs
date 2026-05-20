import fs from 'node:fs'
import path from 'node:path'

const root = process.cwd()
const targets = [
  path.join(root, 'public', 'valaxy-fuse-list.json'),
  path.join(root, 'dist', 'valaxy-fuse-list.json'),
]

function fileFromLink(link) {
  if (!link || link === '/')
    return path.join(root, 'pages', 'index.md')

  const clean = link.replace(/^\//, '').replace(/\/$/, '')
  const asIndex = path.join(root, 'pages', clean, 'index.md')
  const asFile = path.join(root, 'pages', `${clean}.md`)
  if (fs.existsSync(asIndex))
    return asIndex
  if (fs.existsSync(asFile))
    return asFile
  return null
}

function normalize(text) {
  return text
    .replace(/```[\s\S]*?```/g, ' ')
    .replace(/`[^`]*`/g, ' ')
    .replace(/!\[[^\]]*]\([^)]*\)/g, ' ')
    .replace(/\[[^\]]*]\([^)]*\)/g, ' ')
    .replace(/<[^>]+>/g, ' ')
    .replace(/[>#*_~|]/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()
}

function splitBilingual(raw) {
  const enBlocks = []
  const zhBlocks = []

  const blockRe = /:::\s*(en|zh(?:-CN)?)\s*\n([\s\S]*?)\n:::/g
  let m
  while ((m = blockRe.exec(raw)) !== null) {
    if (m[1] === 'en')
      enBlocks.push(m[2])
    else
      zhBlocks.push(m[2])
  }

  return {
    en: normalize(enBlocks.join('\n')),
    zh: normalize(zhBlocks.join('\n')),
    all: normalize(raw),
  }
}

for (const file of targets) {
  if (!fs.existsSync(file))
    continue

  const list = JSON.parse(fs.readFileSync(file, 'utf8'))
  for (const item of list) {
    const mdFile = fileFromLink(item.link)
    if (!mdFile || !fs.existsSync(mdFile))
      continue
    const raw = fs.readFileSync(mdFile, 'utf8')
    const parsed = splitBilingual(raw)
    item.contentEn = parsed.en
    item.contentZh = parsed.zh
    item.content = parsed.all
  }
  fs.writeFileSync(file, JSON.stringify(list))
}

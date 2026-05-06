import type { ThemeConfig } from 'valaxy-theme-nova'
import { defineValaxyConfig } from 'valaxy'
import modManifest from '../mod_manifest.json'

export default defineValaxyConfig<ThemeConfig>({
  theme: 'nova',

  siteConfig: {
    title: 'RitsuLib',
    url: 'https://ritsulib-sts2.local',
    description:
      'RitsuLib — Slay the Spire 2 mod framework: patching, persistence, lifecycle, localization, and authoring helpers',
    lang: 'en',
    languages: ['en', 'zh-CN'],

    author: {
      name: 'OLC',
    },

    search: {
      enable: false,
    },
  },

  themeConfig: {
    colors: {
      primary: '#3D5A80',
    },

    navTitle: { en: 'RitsuLib', 'zh-CN': 'RitsuLib' },

    nav: [
      { locale: 'nav.home', link: '/' },
      {
        locale: 'nav.guide',
        link: '/guide/',
      },
      {
        text: `v${modManifest.version}`,
        link: 'https://github.com/WRXinYue/STS2-RitsuLib/releases',
      },
    ],

    navTools: [['toggleLocale', 'toggleTheme']],

    hero: {
      title: { en: 'RITSULIB', 'zh-CN': 'RITSULIB' },
      motto: {
        en: 'Slay the Spire 2 mod framework — registries, lifecycle, patches & tooling',
        'zh-CN': '《杀戮尖塔 2》模组框架：注册器、生命周期、补丁与工具链',
      },
      img: 'https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp',
    },

    footer: {
      since: 2026,
    },
  },
})

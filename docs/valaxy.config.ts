import type { PressTheme } from 'valaxy-theme-press'
import { defineValaxyConfig } from 'valaxy'

export default defineValaxyConfig<PressTheme.Config>({
  theme: 'press',

  siteConfig: {
    title: 'RitsuLib',
    url: 'https://sts2-ritsulib.ritsukage.com/',
    description:
      'RitsuLib - Slay the Spire 2 mod framework: patching, persistence, lifecycle, localization, and authoring helpers',
    favicon: '',
    lang: 'en',
    languages: ['en', 'zh-CN'],

    author: {
      name: 'OLC',
    },
    excerpt: {
      auto: true,
      length: 4000,
    },

    search: {
      enable: true,
      provider: 'fuse',
    },
    fuse: {
      options: {
        // Match actual valaxy-fuse-list fields for bilingual docs.
        threshold: 0.38,
        ignoreLocation: true,
        minMatchCharLength: 1,
        keys: [
          { name: 'title.en', weight: 0.22 },
          { name: 'title.zh-CN', weight: 0.22 },
          { name: 'title', weight: 0.1 },
          { name: 'excerpt', weight: 0.12 },
          { name: 'contentEn', weight: 0.17 },
          { name: 'contentZh', weight: 0.17 },
          { name: 'content', weight: 0.1 },
        ],
      },
    },
    lastUpdated: true,
  },

  themeConfig: {
    nav: [
      {
        text: 'nav.home',
        link: '/',
      },
      {
        text: 'nav.guide',
        link: '/guide/',
      },
      {
        text: 'nav.releases',
        link: 'https://github.com/BAKAOLC/STS2-RitsuLib/releases',
      },
    ],
    sidebar: [
      {
        text: 'nav.group_getting_started',
        items: [
          { text: 'nav.guide_hub', link: '/guide/' },
          { text: 'nav.guide_getting_started', link: '/guide/getting-started' },
          { text: 'nav.guide_terminology', link: '/guide/terminology' },
          { text: 'nav.guide_framework_design', link: '/guide/framework-design' },
        ],
      },
      {
        text: 'nav.group_content_authoring',
        items: [
          { text: 'nav.guide_content_authoring', link: '/guide/content-authoring-toolkit' },
          { text: 'nav.guide_content_packs', link: '/guide/content-packs-and-registries' },
          { text: 'nav.guide_character_unlock', link: '/guide/character-and-unlock-scaffolding' },
          { text: 'nav.guide_card_dynamic_var', link: '/guide/card-dynamic-var-toolkit' },
          { text: 'nav.guide_custom_events', link: '/guide/custom-events' },
          { text: 'nav.guide_timeline_unlocks', link: '/guide/timeline-and-unlocks' },
          { text: 'nav.guide_asset_profiles', link: '/guide/asset-profiles-and-fallbacks' },
          { text: 'nav.guide_godot_scene', link: '/guide/godot-scene-authoring' },
          { text: 'nav.guide_creature_visuals', link: '/guide/creature-visuals-and-animation' },
        ],
      },
      {
        text: 'nav.group_runtime_apis',
        items: [
          { text: 'nav.guide_persistence', link: '/guide/persistence-guide' },
          { text: 'nav.guide_mod_settings', link: '/guide/mod-settings' },
          { text: 'nav.guide_localization', link: '/guide/localization-and-keywords' },
          { text: 'nav.guide_loc_string', link: '/guide/loc-string-placeholder-resolution' },
          { text: 'nav.guide_lifecycle', link: '/guide/lifecycle-events' },
          { text: 'nav.guide_patching', link: '/guide/patching-guide' },
          { text: 'nav.guide_fmod', link: '/guide/fmod-and-audio' },
          { text: 'nav.guide_shell_theme', link: '/guide/shell-theme' },
          { text: 'nav.guide_debug_log_viewer', link: '/guide/debug-log-viewer' },
          { text: 'nav.guide_update_checks', link: '/guide/update-checks' },
          { text: 'nav.guide_telemetry', link: '/guide/telemetry-backend' },
          { text: 'nav.guide_diagnostics', link: '/guide/diagnostics-and-compatibility' },
        ],
      },
    ],
    socialLinks: [
      { icon: 'i-ri-github-line', link: 'https://github.com/BAKAOLC/STS2-RitsuLib' },
    ],
  },
})

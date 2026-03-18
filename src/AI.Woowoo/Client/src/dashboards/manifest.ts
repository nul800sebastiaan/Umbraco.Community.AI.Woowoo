export const manifests: Array<UmbExtensionManifest> = [
  {
    name: "AIWoowoo Dashboard",
    alias: "AI.Woowoo.Dashboard",
    type: "dashboard",
    js: () => import("./dashboard.element.js"),
    meta: {
      label: "Example Dashboard",
      pathname: "example-dashboard",
    },
    conditions: [
      {
        alias: "Umb.Condition.SectionAlias",
        match: "Umb.Section.Content",
      },
    ],
  },
];

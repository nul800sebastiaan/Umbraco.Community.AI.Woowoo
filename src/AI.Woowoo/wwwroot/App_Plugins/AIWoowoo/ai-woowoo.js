const o = [
  {
    name: "AIWoowoo Entrypoint",
    alias: "AI.Woowoo.Entrypoint",
    type: "backofficeEntryPoint",
    js: () => import("./entrypoint-WAO6U-93.js")
  }
], a = [
  {
    name: "AIWoowoo Dashboard",
    alias: "AI.Woowoo.Dashboard",
    type: "dashboard",
    js: () => import("./dashboard.element-4FMIUip1.js"),
    meta: {
      label: "Example Dashboard",
      pathname: "example-dashboard"
    },
    conditions: [
      {
        alias: "Umb.Condition.SectionAlias",
        match: "Umb.Section.Content"
      }
    ]
  }
], t = [
  ...o,
  ...a
];
export {
  t as manifests
};
//# sourceMappingURL=ai-woowoo.js.map

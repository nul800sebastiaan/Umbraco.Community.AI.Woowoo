export const manifests: Array<UmbExtensionManifest> = [
  {
    name: "AIWoowoo Entrypoint",
    alias: "AI.Woowoo.Entrypoint",
    type: "backofficeEntryPoint",
    js: () => import("./entrypoint.js"),
  },
];

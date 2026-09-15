let apiBaseUrl: string | undefined;

export async function loadRuntimeConfig(): Promise<void> {
  const response = await fetch(`${process.env.BASE_URL}config.json`, {
    cache: "no-store",
  });
  if (!response.ok) {
    throw new Error(
      `Не удалось загрузить config.json (HTTP ${response.status}).`,
    );
  }
  const config: unknown = await response.json();
  const value =
    config && typeof config === "object" && "apiBaseUrl" in config
      ? (config as { apiBaseUrl: unknown }).apiBaseUrl
      : undefined;
  if (typeof value !== "string" || !/^https?:\/\//i.test(value.trim())) {
    throw new Error(
      "В config.json укажите apiBaseUrl: полный HTTP/HTTPS адрес бэкенда.",
    );
  }
  const url = new URL(value.trim());
  if (url.username || url.password || url.search || url.hash) {
    throw new Error(
      "apiBaseUrl не должен содержать логин, пароль или параметры запроса.",
    );
  }
  apiBaseUrl = url.href.replace(/\/+$/, "");
}

export function getApiBaseUrl(): string {
  if (apiBaseUrl === undefined) {
    throw new Error("Настройки фронтенда ещё не загружены.");
  }
  return apiBaseUrl;
}

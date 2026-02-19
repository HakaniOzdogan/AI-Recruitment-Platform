import { expect, Page } from "@playwright/test";

export const E2E_USER = process.env.E2E_USER ?? "admin@local.test";
export const E2E_PASS = process.env.E2E_PASS ?? "Admin123!";

export async function login(page: Page, returnUrl = "/jobs"): Promise<void> {
  await page.goto(`/login?returnUrl=${encodeURIComponent(returnUrl)}`);
  await page.getByLabel("Email").fill(E2E_USER);
  await page.getByLabel("Password").fill(E2E_PASS);
  await page.getByRole("button", { name: "Login" }).click();
  await expect(page).toHaveURL(new RegExp(returnUrl.replace("/", "\\/")));
}

export function uniqueName(prefix: string): string {
  return `${prefix}-${Date.now()}`;
}

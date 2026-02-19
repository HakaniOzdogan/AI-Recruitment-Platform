import { expect, test } from "@playwright/test";
import { login, uniqueName } from "./helpers";

test("job create -> appears in list", async ({ page }) => {
  const title = uniqueName("E2E Job");

  await login(page, "/jobs");

  await page.getByRole("button", { name: /Create Job/i }).first().click();
  await page.getByLabel("Title").fill(title);
  await page.getByLabel("Description").fill("E2E created job for smoke validation");
  await page.getByRole("button", { name: /^Save$/ }).click();

  await expect(page.getByRole("cell", { name: title })).toBeVisible();
});

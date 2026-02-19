import { expect, test } from "@playwright/test";
import { login } from "./helpers";

test("login -> jobs page opens", async ({ page }) => {
  await login(page, "/jobs");
  await expect(page.getByRole("heading", { name: /Jobs/i })).toBeVisible();
});

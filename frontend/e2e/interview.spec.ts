import { expect, test } from "@playwright/test";
import { login, uniqueName } from "./helpers";

test("candidate create -> add to job -> start interview", async ({ page }) => {
  const jobTitle = uniqueName("E2E Interview Job");
  const candidateName = uniqueName("E2E Candidate");

  await login(page, "/jobs");

  await page.getByRole("button", { name: /Create Job/i }).first().click();
  await page.getByLabel("Title").fill(jobTitle);
  await page.getByLabel("Description").fill("Interview flow e2e");
  await page.getByRole("button", { name: /^Save$/ }).click();

  await page.goto("/candidates");
  await page.getByRole("button", { name: /Create Candidate/i }).first().click();
  await page.getByLabel("Full Name").fill(candidateName);
  await page.getByLabel("Email").fill(`${Date.now()}@local.test`);
  await page.getByRole("button", { name: /^Save$/ }).click();

  await page.goto("/jobs");
  const row = page.locator("tr", { hasText: jobTitle }).first();
  await row.getByRole("link", { name: /View/i }).click();

  await page.getByRole("button", { name: /Add candidate to job/i }).first().click();
  const select = page.locator("select").last();
  await select.selectOption({ label: new RegExp(candidateName) });
  await page.getByRole("button", { name: /^Add$/ }).click();

  const appRow = page.locator("tr", { hasText: candidateName }).first();
  const startButton = appRow.getByRole("button", { name: /Start Interview/i });
  if (await startButton.isVisible()) {
    await startButton.click();
  } else {
    await appRow.getByRole("link", { name: /Open Interview/i }).click();
  }

  await expect(page).toHaveURL(/\/interviews\//);
  await expect(page.getByRole("heading", { name: /Interview Session/i })).toBeVisible();
});

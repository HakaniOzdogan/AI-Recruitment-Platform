import { chromium } from "playwright";

const baseUrl = process.env.BASE_URL ?? "http://localhost:5173";
const email = process.env.E2E_USER ?? "smoke-admin@local.test";
const password = process.env.E2E_PASS ?? "Admin123!";
const jobId = process.env.JOB_ID ?? "b5a2a0e8-51b7-4565-9de5-c12e6ea90273";
const sessionId = process.env.SESSION_ID ?? "00000000-0000-0000-0000-000000000000";

const browser = await chromium.launch({ headless: true });
const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });

await page.goto(`${baseUrl}/login`, { waitUntil: "networkidle" });
await page.getByLabel("Email").fill(email);
await page.getByLabel("Password").fill(password);
await page.getByRole("button", { name: "Login" }).click();
await page.waitForURL(/dashboard|jobs|candidates|interviews/, { timeout: 15000 });
await page.getByText("IK Otomasyon", { exact: false }).first().waitFor({ timeout: 10000 });

await page.goto(`${baseUrl}/jobs`, { waitUntil: "networkidle" });
await page.getByRole("heading", { name: "Jobs" }).first().waitFor({ timeout: 10000 });
await page.screenshot({ path: "../screenshots/01-jobs.png", fullPage: true });

await page.goto(`${baseUrl}/jobs/${jobId}`, { waitUntil: "networkidle" });
await page.getByText("Applications", { exact: false }).first().waitFor({ timeout: 10000 });
await page.screenshot({ path: "../screenshots/02-job-detail.png", fullPage: true });

await page.goto(`${baseUrl}/interviews/${sessionId}?jobId=${jobId}`, { waitUntil: "networkidle" });
await page.getByRole("heading", { name: "Interview" }).first().waitFor({ timeout: 10000 });
await page.screenshot({ path: "../screenshots/03-interview.png", fullPage: true });

await browser.close();
console.log("Screenshots captured");

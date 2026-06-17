import cors from "cors";
import express from "express";
import { createActivityCountsRouter } from "./routes/activity-counts.js";
const port = Number.parseInt(process.env.PORT ?? "4000", 10);
const corsOrigin = process.env.CORS_ORIGIN ?? "http://localhost:3000";
const prometheusUrl = process.env.PROMETHEUS_URL ?? "http://prometheus:9090";
const app = express();
app.use(cors({
    origin: corsOrigin,
    methods: ["GET"],
    allowedHeaders: ["Content-Type"],
}));
app.get("/health", (_request, response) => {
    response.json({ status: "ok" });
});
app.use(createActivityCountsRouter(prometheusUrl));
app.listen(port, () => {
    // eslint-disable-next-line no-console
    console.log(`metrics-proxy listening on :${port}`);
});

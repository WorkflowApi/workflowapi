import { load } from "js-yaml";
import type { WorkflowApiDocument } from "../types/workflowapi-model";
import riskEnrichmentYaml from "./risk-enrichment.workflowapi.yaml?raw";

function parseWorkflowApiDocument(yamlContent: string): WorkflowApiDocument {
  const parsed = load(yamlContent);

  if (!parsed || typeof parsed !== "object") {
    throw new Error("Failed to parse WorkflowAPI YAML document.");
  }

  return parsed as WorkflowApiDocument;
}

export const riskEnrichmentDocument: WorkflowApiDocument = parseWorkflowApiDocument(riskEnrichmentYaml);

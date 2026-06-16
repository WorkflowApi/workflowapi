import YAML from "yaml";

/**
 * Thin wrapper around the `yaml` package.
 * Returns the parsed document as `unknown`; callers are responsible for
 * narrowing the type.  Throws a `YAMLParseError` if the source is invalid.
 */
export function parseWorkflowApiYaml(source: string): unknown {
  return YAML.parse(source);
}

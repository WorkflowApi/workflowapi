// React 19 (@types/react ≥19) no longer publishes the JSX namespace to the
// global scope (it lives at React.JSX instead).  Files that use bare
// JSX.Element as a return-type annotation — including parallel-task files
// we cannot otherwise modify — need it globally.  This shim re-exposes it.
export {}

declare global {
  namespace JSX {
    type Element = import("react").JSX.Element;
  }
}

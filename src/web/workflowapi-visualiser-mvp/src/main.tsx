// @xyflow/react ships its styles separately. This import MUST come first —
// omitting it renders the graph as invisible boxes with no edges.
import '@xyflow/react/dist/style.css'

import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)

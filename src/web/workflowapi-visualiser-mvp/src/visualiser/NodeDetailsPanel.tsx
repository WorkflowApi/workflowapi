import type { WorkflowGraphNode } from '../workflowapi/graphTypes'
import { nodeKindColors, nodeKindIcons } from './visualStyles'

export interface NodeDetailsPanelProps {
  selectedNode?: WorkflowGraphNode
}

export default function NodeDetailsPanel({ selectedNode }: NodeDetailsPanelProps): JSX.Element {
  if (!selectedNode) {
    return (
      <div className="p-4 h-full overflow-auto flex items-center justify-center">
        <p className="text-sm text-slate-400 text-center px-6">
          Select a node to inspect its WorkflowAPI source fragment.
        </p>
      </div>
    )
  }

  const color = nodeKindColors[selectedNode.kind]
  const Icon = nodeKindIcons[selectedNode.kind]

  return (
    <div className="p-4 h-full overflow-auto">
      <div className="space-y-4">
        {/* Header */}
        <div
          className="flex items-center gap-2 pb-3 border-b border-slate-200"
          style={{ borderLeft: `4px solid ${color}`, paddingLeft: '0.75rem' }}
        >
          <Icon size={18} style={{ color }} />
          <span className="text-base font-semibold text-slate-800 truncate">
            {selectedNode.label}
          </span>
        </div>

        {/* Kind */}
        <div>
          <dt className="text-xs font-medium text-slate-500 uppercase tracking-wide mb-1">
            Kind
          </dt>
          <dd>
            <span
              className="inline-block text-xs font-medium px-2 py-0.5 rounded-full text-white"
              style={{ backgroundColor: color }}
            >
              {selectedNode.kind}
            </span>
          </dd>
        </div>

        {/* Description */}
        {selectedNode.description && (
          <div>
            <dt className="text-xs font-medium text-slate-500 uppercase tracking-wide mb-1">
              Description
            </dt>
            <dd className="text-sm text-slate-700 bg-slate-50 rounded p-2 leading-relaxed">
              {selectedNode.description}
            </dd>
          </div>
        )}

        {/* Source path */}
        {selectedNode.sourcePath && (
          <div>
            <dt className="text-xs font-medium text-slate-500 uppercase tracking-wide mb-1">
              Source Path
            </dt>
            <dd className="text-xs font-mono text-slate-600 bg-slate-50 rounded px-2 py-1 break-all">
              {selectedNode.sourcePath}
            </dd>
          </div>
        )}

        {/* Raw fragment */}
        <div>
          <dt className="text-xs font-medium text-slate-500 uppercase tracking-wide mb-1">
            Raw Fragment
          </dt>
          <dd>
            <pre className="text-xs bg-slate-50 p-3 rounded overflow-auto">
              {JSON.stringify(selectedNode.raw, null, 2)}
            </pre>
          </dd>
        </div>
      </div>
    </div>
  )
}

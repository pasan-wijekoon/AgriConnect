import { useEffect, useEffectEvent, useRef, type ReactNode } from 'react'

import { Icon } from './ui.tsx'

/**
 * Side panel built on <dialog>: showModal() gives focus trapping, Escape to close and an
 * inert page behind it without extra code.
 */
export function Drawer({ title, open, onClose, children, footer }: {
  title: string
  open: boolean
  onClose: () => void
  children: ReactNode
  footer?: ReactNode
}) {
  const ref = useRef<HTMLDialogElement>(null)
  const handleClose = useEffectEvent(onClose)

  // Other close requests (e.g. Android's back gesture) close the dialog natively. The browser
  // delivers that 'close' event as a queued task, and hidden pages can hold it back, so the
  // buttons and Escape below call onClose directly instead of relying on it.
  useEffect(() => {
    const dialog = ref.current
    if (!dialog) return
    const notify = () => handleClose()
    dialog.addEventListener('close', notify)
    return () => dialog.removeEventListener('close', notify)
  }, [])

  useEffect(() => {
    const dialog = ref.current
    if (!dialog) return
    if (open && !dialog.open) dialog.showModal()
    if (!open && dialog.open) dialog.close()
  }, [open])

  return (
    <dialog
      ref={ref}
      className="drawer"
      aria-label={title}
      onClick={(e) => {
        // A click on the backdrop lands on the <dialog> element itself.
        if (e.target === e.currentTarget) onClose()
      }}
      onKeyDown={(e) => {
        if (e.key === 'Escape') {
          e.preventDefault()
          onClose()
        }
      }}
    >
      <div className="drawer-inner">
        <div className="drawer-head">
          <h2>{title}</h2>
          <button type="button" className="btn btn-quiet btn-icon" onClick={onClose} aria-label="Close">
            <Icon name="close" />
          </button>
        </div>
        <div className="drawer-body">{children}</div>
        {footer && <div className="drawer-foot">{footer}</div>}
      </div>
    </dialog>
  )
}

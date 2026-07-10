"use client"

import { Circle } from "lucide-react"
import { motion } from "motion/react"
import { type ReactNode, useRef, useState } from "react"

import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"

/**
 * RadialMenu — Generic radial menu component.
 *
 * Last updated: 2026-01-21T09:34:10.306Z
 *
 * Props:
 * - arc (number, default 120): total fan angle in degrees. When `angles` is not provided,
 *   items are placed from `startAngle` across `arc` evenly (step = arc / (n-1)).
 * - startAngle (number, default 210): starting angle in degrees. Coordinate system: 0° = right, positive = clockwise.
 * - angles (number[], optional): explicit absolute angles for each item (degrees). If provided, `arc`/`startAngle` are ignored.
 * - items ({ label: ReactNode; onClick?: ()=>void }[], default: 3 language labels): menu items; `label` accepts any ReactNode (icons, text, or both).
 * - trigger ("hover" | "click" | "both", default "both"): how the menu opens (hover, click, or both).
 * - toggle (ReactNode, optional): custom toggle content rendered inside the toggle button (falls back to a globe emoji).
 * - toggleAriaLabel (string, default "Open menu"): aria-label for the toggle button.
 * - radius (number, px, optional): distance from toggle center to items; defaults to 72px.
 *
 * Behavior / implementation notes:
 * - Items orbit around the toggle using a rotation + translateX technique to guarantee identical radius for every item.
 * - Each item's content is counter-rotated so it remains visually upright.
 * - To center the fan on a specific direction `center`, set `startAngle = center - arc/2` (e.g. for down use center=90° → startAngle = 90 - arc/2).
 * - Clicking an item runs its `onClick` (if any) and closes the menu.
 *
 * Accessibility:
 * - The toggle button gets its aria-label from `toggleAriaLabel`; items are rendered as buttons and should be focusable.
 */

const RADIUS = 72 // single source of truth (px)

type RadialMenuProps = {
  arc?: number
  startAngle?: number
  trigger?: "hover" | "click" | "both"
  angles?: number[]
  items?: { label: ReactNode, onClick?: () => void }[]
  toggle?: ReactNode
  toggleAriaLabel?: string
  toggleClassName?: string
  radius?: number
}

const RadialMenu = ({
  arc = 120,
  startAngle = 210,
  trigger = "both",
  angles,
  items,
  toggle,
  toggleAriaLabel = "Open menu",
  toggleClassName,
  radius: radiusProp,
}: RadialMenuProps) => {
  const [open, setOpen] = useState(false)

  const hoverEnabled = trigger !== "click"
  const clickEnabled = trigger !== "hover"

  const defaultItems: { label: ReactNode, onClick?: () => void }[] = [{ label: "EN" }, { label: "JA" }, { label: "KO" }]
  const itemsList = items && items.length ? items : defaultItems
  const step = itemsList.length > 1 ? arc / (itemsList.length - 1) : 0
  const anglesList = Array.isArray(angles) && angles.length === itemsList.length
    ? angles
    : itemsList.map((_, i) => startAngle + step * i)
  const radius = typeof radiusProp === "number" ? radiusProp : RADIUS

  return (
    <div className="relative w-9 h-9">
      {/* toggle */}
      <Button
        variant="ghost"
        aria-label={toggleAriaLabel}
        onClick={() => { if (clickEnabled) setOpen(v => !v) }}
        onMouseEnter={() => { if (hoverEnabled) setOpen(true) }}
        onMouseLeave={() => { if (hoverEnabled) setOpen(false) }}
        className={cn(
          "relative z-[60] h-9 w-9 rounded-full hover:bg-accent hover:text-accent-foreground transition-colors",
          open && "bg-accent text-accent-foreground",
          toggleClassName
        )}
      >
        {toggle ?? <span className="text-sm"><Circle /></span>}
      </Button>

      {/* radial items - absolute positioned, no layout impact */}
      {itemsList.map((item, i) => {
        const angle = anglesList[i]

        return (
          <motion.div
            key={String(item.label) + i}
            className="absolute left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 z-50"
            initial={{ rotate: 0, opacity: 0 }}
            animate={
              open
                ? { rotate: angle, opacity: 1 }
                : { rotate: 0, opacity: 0 }
            }
            transition={{
              type: "spring",
              stiffness: 360,
              damping: 26,
              delay: i * 0.05,
            }}
            style={{ pointerEvents: open ? 'auto' : 'none' }}
          >
            {/* orbital arm → guarantees SAME radius */}
            <div style={{ transform: `translateX(${radius}px)` }}>
              {/* counter-rotation keeps content upright (0°) */}
              <motion.div
                style={{ rotate: -angle }}
                className="-translate-x-1/2 -translate-y-1/2"
              >
                <Button
                  variant="outline"
                  className="h-8 w-8 min-h-8 min-w-8 max-h-8 max-w-8 shrink-0 flex-none rounded-full text-xs font-medium shadow-lg border-2 border-primary/30 bg-background hover:bg-primary hover:text-primary-foreground hover:border-primary transition-all p-0 flex items-center justify-center"
                  onClick={() => { (item as { onClick?: () => void }).onClick?.(); setOpen(false) }}
                >
                  {item.label}
                </Button>
              </motion.div>
            </div>
          </motion.div>
        )
      })}
    </div>
  )
}

export default RadialMenu

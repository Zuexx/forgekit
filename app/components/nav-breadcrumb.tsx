'use client'

import { usePathname } from 'next/navigation'
import { useLocale } from 'next-intl'

import { LocaleSwitcher } from '@/components/locale-switcher'
import { ThemeSwitcher } from '@/components/theme-switcher'
import {
    Breadcrumb,
    BreadcrumbItem,
    BreadcrumbLink,
    BreadcrumbList,
    BreadcrumbPage,
    BreadcrumbSeparator,
} from '@/components/ui/breadcrumb'

export const NavBreadCrumb = () => {

    // TODO: friendly names for breadcrumb and multiple language supported
    // const labelMap: Record<string, string> = {
    //     products: 'Our Products',
    //     'about-us': 'About Us',
    //     'contact': 'Contact Us',
    // };

    const locale = useLocale()
    const pathname = usePathname()

    const segments = pathname.replace(`/${locale}/`, '/').split('/').filter(segment => segment)

    const getLabel = (segment: string) => {
        return segment.replace(/-/g, ' ').replace(/\b\w/g, c => c.toUpperCase())
    }

    return (
        <div className="flex items-center justify-between w-full overflow-x-clip">
            <section className="flex-1 min-w-0 overflow-hidden">
                <Breadcrumb>
                    <BreadcrumbList>
                        <BreadcrumbItem className="hidden md:block">
                            <BreadcrumbLink href="/">Home</BreadcrumbLink>
                        </BreadcrumbItem>

                        {segments.map((segment, index) => {
                            const href = `/${locale}/` + segments.slice(0, index + 1).join('/')

                            return (
                                <div key={href} className="flex items-center">
                                    <BreadcrumbSeparator className="hidden md:block" />
                                    <BreadcrumbItem>
                                        <BreadcrumbPage>
                                            {getLabel(segment)}
                                        </BreadcrumbPage>
                                    </BreadcrumbItem>
                                </div>
                            )
                        })}
                    </BreadcrumbList>
                </Breadcrumb>
            </section>
            <section className="flex gap-2 shrink-0 ml-4">
                <ThemeSwitcher />
                <LocaleSwitcher arc={75} startAngle={125} />
            </section>
        </div>
    )
}

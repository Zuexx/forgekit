import { setRequestLocale } from "next-intl/server"

import { AppSidebar } from "@/components/app-siderbar"
import { NavBreadCrumb } from "@/components/nav-breadcrumb"
import { Separator } from "@/components/ui/separator"
import {
    SidebarInset,
    SidebarProvider,
    SidebarTrigger,
} from "@/components/ui/sidebar"

type AdminLayoutProps = {
    children: React.ReactNode
    params: Promise<{ locale: string }>
}

export const AdminLayout = async ({ children, params }: AdminLayoutProps) => {
    const { locale } = await params
    setRequestLocale(locale)

    return (
        <SidebarProvider>
            <AppSidebar />
            <SidebarInset>
                <header className="flex h-16 shrink-0 items-center gap-2 transition-[width,height] ease-linear group-has-data-[collapsible=icon]/sidebar-wrapper:h-12">
                    <div className="flex items-center gap-2 px-4 w-full">
                        <SidebarTrigger className="-ml-1" />
                        <Separator
                            orientation="vertical"
                            className="mr-2 data-[orientation=vertical]:h-4"
                        />
                        <NavBreadCrumb />
                    </div>
                </header>
                <div className="flex flex-1 flex-col gap-4 p-4 pt-0">
                    {children}
                </div>
            </SidebarInset>
        </SidebarProvider>
    )
}

export default AdminLayout

import { useState, useEffect } from "react"  // Add useEffect here
import "./App.css"
import { evaluate } from '@mdx-js/mdx'
import * as runtime from 'react/jsx-runtime'
import { Button } from "@/components/ui/button"
import { Bar, BarChart } from "recharts"
import { ChartConfig, ChartContainer } from "@/components/ui/chart"
import * as ts from "typescript"
// Import the shadcn dropdown menu components
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"




const chartData = [
    { month: "January", desktop: 186, mobile: 80 },
    { month: "February", desktop: 305, mobile: 200 },
    { month: "March", desktop: 237, mobile: 120 },
    { month: "April", desktop: 73, mobile: 190 },
    { month: "May", desktop: 209, mobile: 130 },
    { month: "June", desktop: 214, mobile: 140 },
]

const chartConfig = {
    desktop: {
        label: "Desktop",
        color: "#2563eb",
    },
    mobile: {
        label: "Mobile",
        color: "#60a5fa",
    },
} satisfies ChartConfig

function App() {
    const buttonText: string = `TypeScript Version: ${ts.version}`;
    const [Content, setContent] = useState<React.ComponentType | null>(null)

    // State to store the returned models along with the selected model.
    const [models, setModels] = useState<string[]>([])
    const [selectedModel, setSelectedModel] = useState<string>("Select Model")

    useEffect(() => {
        const compileMdx = async () => {
            const mdxContent = `
# Dynamic MDX

This content was generated programmatically!

- Item 1
- Item 2
      `

            const { default: Component } = await evaluate(mdxContent, {
                ...runtime,
                development: false
            })

            setContent(() => Component)
        }

        compileMdx()
    }, []);

    // Updated fetch call that extracts the models from the response
    const makeTestCall = async () => {
        try {
            const response = await fetch("/api/test", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
            })
            const data = await response.json()
            // If the response is successful and we have a models array, store it in state.
            if (data.success && Array.isArray(data.models)) {
                setModels(data.models)
                // Optionally, you could reset the selected model:
                setSelectedModel("Select Model")
            }
        } catch (error) {
            console.error("Error fetching test data:", error)
            setModels([])
        }
    }

    return (
        <>
            <div className="p-4">
                <ChartContainer config={chartConfig} className="h-[200px] w-[300px]">
                    <BarChart accessibilityLayer data={chartData}>
                        <Bar dataKey="desktop" fill="var(--color-desktop)" radius={4} />
                        <Bar dataKey="mobile" fill="var(--color-mobile)" radius={4} />
                    </BarChart>
                </ChartContainer>
                <div className="mt-4">
                    <Button className="bg-teal-500 hover:bg-teal-600 text-white">
                        {buttonText}
                    </Button>
                    <Button onClick={makeTestCall} className="ml-4">
                        Test Server Call
                    </Button>
                </div>
                {/* Always display the dropdown menu with appropriate content */}
                <div className="mt-4">
                    <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                            <Button variant="outline">{selectedModel}</Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent>
                            {models.length > 0 ? (
                                models.map((model, index) => (
                                    <DropdownMenuItem
                                        key={index}
                                        onSelect={() => setSelectedModel(model)}
                                    >
                                        {model}
                                    </DropdownMenuItem>
                                ))
                            ) : (
                                <DropdownMenuItem disabled>
                                    No models available
                                </DropdownMenuItem>
                            )}
                        </DropdownMenuContent>
                    </DropdownMenu>
                </div>
                <div className="mt-4 prose dark:prose-invert">
                    {Content && <Content />}
                </div>
            </div>
        </>
    )
}

export default App
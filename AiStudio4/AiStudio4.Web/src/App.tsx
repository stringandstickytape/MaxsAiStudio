import './App.css'
import { Button } from "@/components/ui/button"
import { Bar, BarChart } from "recharts"
import { ChartConfig, ChartContainer } from "@/components/ui/chart"
import { useState } from 'react'
import * as ts from 'typescript';

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
    const [testData, setTestData] = useState<string>('');

    const makeTestCall = async () => {
        try {
            const response = await fetch('/api/test', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            });
            const data = await response.json();

            // data is like this:
            /*
            {
                "success": true,
                    "models": [
                        "claude-3-5-sonnet-latest",
                        "gemini-2.0-flash-exp",
                        ...
                        */

            setTestData(JSON.stringify(data, null, 2));
        } catch (error) {
            console.error('Error fetching test data:', error);
            setTestData('Error fetching data');
        }
    };

  return (
      <>
      <div>
        <ChartContainer config= { chartConfig } className = "h-[200px] w-[300px]" >
            <BarChart accessibilityLayer data = { chartData } >
                <Bar dataKey="desktop" fill = "var(--color-desktop)" radius = { 4} />
                    <Bar dataKey="mobile" fill = "var(--color-mobile)" radius = { 4} />
                        </BarChart>
                        </ChartContainer>
              <Button className="bg-teal-500 hover:bg-teal-600 text-white">{buttonText}</Button>
        <Button onClick={makeTestCall} className="ml-4">Test Server Call</Button>
        {testData && (
          <pre className="mt-4 p-4 bg-gray-100 rounded">
            {testData}
          </pre>
        )}
      </div>

    </>
  )
}

export default App
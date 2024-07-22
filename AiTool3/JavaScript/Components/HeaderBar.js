const { useState } = React;

const HeaderBar = () => {
    const [logoText, setLogoText] = useState("AI Studio");

    const changeLogo = (newText) => {
        setLogoText(newText);
    };

    // export changeLogo
    window.changeLogo = changeLogo;

    return (
        <>
            <style>
                {`
                    .header-bar {
                        display: flex;
                        justify-content: space-between;
                        align-items: center;
                        background-color: #333;
                        color: white;
                        padding: 10px 20px;
                    }
                    .logo {
                        font-size: 24px;
                        font-weight: bold;
                    }
                    .nav-items {
                        display: flex;
                        gap: 20px;
                    }
                    .search-bar {
                        padding: 5px 10px;
                        border-radius: 5px;
                        border: none;
                    }
                `}
            </style>

            <div className="header-bar">
                <div className="logo">{logoText}</div>
                <div className="nav-items">
                    <AiStudioButton label="Tools" color="#4CAF50" dropdownItems={["Find-and-replace", "Tool2"]} />
                    <AiStudioButton label="Send" color="#008CBA" />
                    <AiStudioButton label="Cancel" color="#f44336" />
                    <AiStudioButton label="New" color="#555555" dropdownItems={["New with context", "New with prompt"]} />
                    <input type="text" className="search-bar" placeholder="Search..." />
                </div>
            </div>
        </>
    );
};
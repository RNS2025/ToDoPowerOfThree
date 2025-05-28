# ⚡ The Power of Three - Cyberpunk Task Manager

A futuristic, cyberpunk-styled task management application built with Blazor WebAssembly. Focus on the "Power of Three" concept - generating and completing 3 daily tasks to maximize productivity.

## 🚀 Features

### Core "Power of Three" Functionality

- **Daily Task Generation**: Generate 3 tasks from your task list using different algorithms
- **Smart Algorithms**:
  - ⚖️ **Balanced**: 1 High, 1 Medium, 1 Low priority task
  - 🔥 **Priority First**: Most important tasks first
  - ⏰ **Deadline Focus**: Most urgent tasks by deadline
  - 🎲 **Random**: Surprise selection for variety

### Task Management

- **Full CRUD Operations**: Create, Read, Update, Delete tasks
- **Rich Task Properties**:
  - Title and description
  - Subtasks for detailed breakdowns
  - Priority levels (High, Medium, Low)
  - Deadline tracking
  - Time estimation in minutes
  - Categories for organization
- **Real-time Updates**: Live UI updates across all components
- **Persistent Storage**: Tasks saved in browser localStorage

### Cyberpunk UI/UX

- **Futuristic Design**: Neon colors, glass morphism, animated backgrounds
- **Interactive Elements**: Glowing buttons, hover effects, completion celebrations
- **Responsive Layout**: Works on desktop and mobile devices
- **Accessibility**: Keyboard navigation and screen reader friendly

## 🎯 How to Use

1. **Add Tasks**: Click "Add New Mission" to create your tasks
2. **Generate Daily Focus**: Use the "Power of Three" section to select 3 tasks for today
3. **Complete Tasks**: Check off completed tasks to track progress
4. **Manage Tasks**: Edit, delete, or filter your task list as needed

## 🛠 Technical Stack

- **Framework**: Blazor WebAssembly (.NET 9.0)
- **Storage**: Blazored LocalStorage
- **Styling**: Custom CSS with cyberpunk theme
- **Fonts**: Orbitron and Rajdhani from Google Fonts
- **Architecture**: Component-based with centralized service management

## 🚀 Getting Started

```bash
# Clone and navigate to project
cd PowerOfThree

# Restore dependencies
dotnet restore

# Run the application
dotnet run

# Open browser to http://localhost:5000
```

## 🎨 Design Philosophy

The "Power of Three" concept is based on psychological research showing that focusing on 3 key tasks per day leads to:

- Better focus and reduced overwhelm
- Higher completion rates
- Improved sense of accomplishment
- Sustainable productivity habits

The cyberpunk aesthetic creates an engaging, futuristic experience that makes task management feel like a mission rather than a chore.

## 📱 Browser Compatibility

- Chrome/Edge (recommended)
- Firefox
- Safari
- Modern mobile browsers

## 🔧 Architecture

```
PowerOfThree/
├── Components/           # Reusable Blazor components
│   ├── TaskForm.razor   # Task creation/editing
│   ├── TaskList.razor   # Task management and filtering
│   └── TodayTasks.razor # Daily task generation and completion
├── Models/              # Data models
│   └── TodoTask.cs      # Task model with enums
├── Services/            # Business logic
│   └── TaskManager.cs   # Centralized task management
├── Pages/               # Application pages
│   └── Index.razor      # Main application page
└── wwwroot/css/         # Styling
    └── app.css          # Cyberpunk theme styles
```

## 🎉 Mission Accomplished!

Your cyberpunk task management system is ready to help you conquer your daily goals with the Power of Three!

using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Zakarni.Core.Models;
using Zakarni.Core.Interfaces;
using System.Linq;

namespace Zakarni.UI.ViewModels;

public partial class TodoViewModel : ObservableObject
{
    private readonly ISettingsService _settings;

    [ObservableProperty] private string _newTaskText = string.Empty;
    [ObservableProperty] private int _selectedCategoryIndex = 0;
    
    public ObservableCollection<TodoItem> Tasks { get; } = new();

    public TodoViewModel(ISettingsService settings)
    {
        _settings = settings;
        LoadTasks();
    }

    private void LoadTasks()
    {
        Tasks.Clear();
        var sorted = _settings.TodoList
            .OrderBy(t => t.IsCompleted)
            .ThenByDescending(t => t.Priority)
            .ThenByDescending(t => t.CreatedAt);

        foreach (var item in sorted)
        {
            Tasks.Add(item);
        }
    }

    [RelayCommand]
    private void AddTask()
    {
        if (string.IsNullOrWhiteSpace(NewTaskText)) return;
        
        var category = (TodoCategory)SelectedCategoryIndex;
        var item = new TodoItem 
        { 
            Task = NewTaskText, 
            Category = category, 
            Priority = category == TodoCategory.Spiritual ? TodoPriority.High : TodoPriority.Medium 
        };

        _settings.TodoList.Add(item);
        _settings.Save();
        
        NewTaskText = string.Empty;
        LoadTasks();
    }

    [RelayCommand]
    private void DeleteTask(TodoItem item)
    {
        if (item == null) return;
        _settings.TodoList.Remove(item);
        _settings.Save();
        LoadTasks();
    }

    [RelayCommand]
    private void ToggleTask(TodoItem item)
    {
        if (item == null) return;
        item.IsCompleted = !item.IsCompleted;
        _settings.Save();
        LoadTasks();
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Avalonia.Controls;
using SubRenamer.Helper;
using SubRenamer.Model;

namespace SubRenamer.Services;

public class RenameService(Window target) : IRenameService
{
    private readonly Window _target = target;

    public void UpdateRenameTaskList(IEnumerable<MatchItem> matchList, Collection<RenameTask> destList)
    {
        destList.Clear();

        foreach (var item in matchList)
        {
            if (string.IsNullOrEmpty(item.Subtitle) || string.IsNullOrEmpty(item.Video)) continue;

            var alter = "/" + Path.GetFileNameWithoutExtension(item.Video) +
                        Path.GetExtension(item.Subtitle);

            if(Config.Get().RenameStrategy == Common.RenameStrategy.Copy)
            {
                alter = Path.GetDirectoryName(item.Video) + alter;
            } else
            {
                alter = Path.GetDirectoryName(item.Subtitle) + alter;
            }

            destList.Add(new RenameTask(item.Subtitle, alter, item.Status == "已修改" ? "已修改" : "待修改")
            {
                MatchItem = item
            });
        }
    }

    public void ExecuteRename(IEnumerable<RenameTask> taskList)
    {
        foreach (var task in taskList)
        {
            if (task.Status == "已修改") continue;
            if (task.Status == "已跳过") continue;
            
            try
            {
                if (Config.Get().RenameStrategy == Common.RenameStrategy.Copy)
                {
                    FileHelper.CopyFile(task.Origin, task.Alter);
                } else
                {
                    if (Config.Get().Backup) FileHelper.BackupFile(task.Origin);
                    FileHelper.RenameFile(task.Origin, task.Alter);
                }
                task.Status = "已修改";
                if (task.MatchItem != null) task.MatchItem.Status = "已修改";
            }
            catch (Exception e)
            {
                task.Status = $"失败：{e.Message}";
                // task.Error = e.Message;
            }
        }
    }

    public string GenerateRenameCommands(IEnumerable<MatchItem> list)
    {
        var command = "";
        
        foreach (var item in list)
        {
            var subtitle = !string.IsNullOrEmpty(item.Subtitle) ? item.Subtitle : "?";
            var video = !string.IsNullOrEmpty(item.Video) ? item.Video : "?";
            command += $"mv {subtitle} {video}\n";
        }

        return command.Trim();
    }
}
package com.vrexperienceagb.importer;

import android.app.Activity;
import android.app.Fragment;
import android.content.Intent;
import android.database.Cursor;
import android.net.Uri;
import android.os.Bundle;
import android.provider.OpenableColumns;
import com.unity3d.player.UnityPlayer;
import org.json.JSONObject;
import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;

/** Copies a user-selected document into cache; never requires broad storage permission. */
public final class JsonFilePicker extends Fragment {
    public static boolean available() {
        Intent intent=new Intent(Intent.ACTION_OPEN_DOCUMENT);intent.addCategory(Intent.CATEGORY_OPENABLE);intent.setType("*/*");
        return intent.resolveActivity(UnityPlayer.currentActivity.getPackageManager())!=null;
    }
    public static void open(final String receiver) {
        final Activity activity=UnityPlayer.currentActivity;
        activity.runOnUiThread(() -> {
            JsonFilePicker picker=new JsonFilePicker();
            Bundle args=new Bundle();args.putString("receiver",receiver);picker.setArguments(args);
            activity.getFragmentManager().beginTransaction().add(picker,"VRJsonPicker").commit();
        });
    }
    @Override public void onCreate(Bundle saved) {
        super.onCreate(saved);
        if(saved!=null)return;
        Intent intent=new Intent(Intent.ACTION_OPEN_DOCUMENT);
        intent.addCategory(Intent.CATEGORY_OPENABLE);
        // Some exporters label JSON as text/plain or application/octet-stream. Validate content in C#.
        intent.setType("*/*");
        try{startActivityForResult(intent,901);}catch(Exception e){finish("","","File picker unavailable. Copy JSON into Models / Profiles and use Refresh.");}
    }
    @Override public void onActivityResult(int request,int result,Intent data) {
        if(request!=901)return;
        if(result!=Activity.RESULT_OK || data==null || data.getData()==null){finish("","","Import cancelled.");return;}
        final Uri uri=data.getData();final Activity activity=getActivity();
        new Thread(() -> {
            File temporary=null;
            try {
                String name="import.json";
                try(Cursor cursor=activity.getContentResolver().query(uri,new String[]{OpenableColumns.DISPLAY_NAME},null,null,null)){
                    if(cursor!=null && cursor.moveToFirst())name=cursor.getString(0);
                }
                temporary=File.createTempFile("vr-json-",".json",activity.getCacheDir());
                try(InputStream input=activity.getContentResolver().openInputStream(uri);FileOutputStream output=new FileOutputStream(temporary)) {
                    if(input==null)throw new Exception();
                    byte[] buffer=new byte[16384];int count,total=0;
                    while((count=input.read(buffer))!=-1){total+=count;if(total>32*1024*1024)throw new Exception();output.write(buffer,0,count);}
                }
                finish(temporary.getAbsolutePath(),name,"");
            }catch(Exception e){if(temporary!=null)temporary.delete();finish("","","Cannot read document. Choose a readable JSON file up to 32 MiB.");}
        },"VRJsonImport").start();
    }
    private void finish(String path,String name,String error) {
        try {
            JSONObject result=new JSONObject();result.put("path",path);result.put("name",name);result.put("error",error);
            UnityPlayer.UnitySendMessage(getArguments().getString("receiver"),"OnJsonFilePicked",result.toString());
        }catch(Exception ignored){}
        Activity activity=getActivity();
        if(activity!=null)activity.runOnUiThread(() -> activity.getFragmentManager().beginTransaction().remove(this).commitAllowingStateLoss());
    }
}

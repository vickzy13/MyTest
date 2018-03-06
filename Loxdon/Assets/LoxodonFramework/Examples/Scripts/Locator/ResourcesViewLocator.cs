﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Loxodon.Framework.Asynchronous;
using Loxodon.Framework.Views;

namespace Loxodon.Framework.Examples
{
	public class ResourcesViewLocator : IUIViewLocator
	{
		private GlobalWindowManager globalWindowManager;
		private Dictionary<string, WeakReference> templates = new Dictionary<string, WeakReference> ();

		protected string Normalize (string name)
		{
			int index = name.IndexOf ('.');
			if (index < 0)
				return name;

			return name.Substring (0, index);
		}

		public virtual T LoadView<T> (string name) where T : IView
		{
			name = Normalize (name);

			WeakReference weakRef;
			GameObject viewTemplateGo;
			if (this.templates.TryGetValue (name, out weakRef) && weakRef.IsAlive) {
				viewTemplateGo = (GameObject)weakRef.Target;
			} else {
				viewTemplateGo = Resources.Load<GameObject> (name);
				if (viewTemplateGo != null) {
					viewTemplateGo.SetActive (false);
					this.templates [name] = new WeakReference (viewTemplateGo);
				}
			}

			if (viewTemplateGo == null || viewTemplateGo.GetComponent<T> () == null)
				return default(T);

			GameObject go = GameObject.Instantiate (viewTemplateGo);
			go.name = viewTemplateGo.name;
			return go.GetComponent<T> ();
		}

		public virtual IProgressTask<float, T> LoadViewAsync<T> (string name) where T : IView
		{
			ProgressTask<float, T> task = new ProgressTask<float, T> (p => DoLoad<T> (p, name));
			return task.Start (30);
		}

		protected virtual IEnumerator DoLoad<T> (IProgressPromise<float, T> promise, string name)
		{
			name = Normalize (name);

			WeakReference weakRef;
			GameObject viewTemplateGo;
			if (this.templates.TryGetValue (name, out weakRef) && weakRef.IsAlive) {
				viewTemplateGo = (GameObject)weakRef.Target;
			} else {
				ResourceRequest request = Resources.LoadAsync<GameObject> (name);
				if (!request.isDone) {
					promise.UpdateProgress (request.progress);
					yield return null;
				}

				viewTemplateGo = (GameObject)request.asset;
				if (viewTemplateGo != null) {
					viewTemplateGo.SetActive (false);
					this.templates [name] = new WeakReference (viewTemplateGo);
				}
			}

			if (viewTemplateGo == null || viewTemplateGo.GetComponent<T> () == null) {
				promise.UpdateProgress (1f);
				promise.SetException (new NotFoundException (name));
				yield break;
			}

			GameObject go = GameObject.Instantiate (viewTemplateGo);
			go.name = viewTemplateGo.name;
			promise.UpdateProgress (1f);
			promise.SetResult (go.GetComponent<T> ());
		}

		protected virtual IWindowManager GetDefaultWindowManager ()
		{
			if (globalWindowManager != null)
				return globalWindowManager;

			globalWindowManager = GameObject.FindObjectOfType<GlobalWindowManager> ();
			if (globalWindowManager == null)
				throw new NotFoundException ("GlobalWindowManager");

			return globalWindowManager;
		}

		public virtual T LoadWindow<T> (string name) where T : Window
		{
			return LoadWindow<T> (null, name);
		}

		public virtual T LoadWindow<T> (IWindowManager windowManager, string name) where T : Window
		{
			if (windowManager == null)
				windowManager = this.GetDefaultWindowManager ();

			T target = this.LoadView<T> (name);
			if (target != null) {
				target.WindowManager = windowManager;
			}

			return target;
		}

		public virtual IProgressTask<float, T> LoadWindowAsync<T> (string name) where T : Window
		{
			return this.LoadWindowAsync<T> (null, name);
		}

		public virtual IProgressTask<float, T> LoadWindowAsync<T> (IWindowManager windowManager, string name) where T : Window
		{
			if (windowManager == null)
				windowManager = this.GetDefaultWindowManager ();

			ProgressTask<float, T> task = new ProgressTask<float, T> (p => DoLoad<T> (p, name));
			return task.Start (30).OnPostExecute (win => {
				win.WindowManager = windowManager;
			});
		}
	}
}
